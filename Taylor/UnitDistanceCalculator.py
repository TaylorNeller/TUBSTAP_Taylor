# TODO unobstructed_distance is the *first* distance within movement range found, not the shortest distance
# TODO uses swapped x,y coordinates but this shouldn't matter
# TODO run check with this against other calculator to see if this works
# TODO no support for rectangular maps yet
# TODO remove debug prints

import numpy as np
from numba import jit
from concurrent.futures import ThreadPoolExecutor
import multiprocessing as mp
from typing import List, Tuple, Any, Dict, Set
import heapq
from dataclasses import dataclass

@dataclass
class GCNRequest:
    units: List[Any]
    map_data: np.ndarray
    max_units: int

class UnitExploration:
    """Maintains exploration state for a single unit's position"""
    def __init__(self, map_size: int):
        self.map_size = map_size
        self.g_scores = np.full((map_size, map_size), np.inf)
        self.closed_set = np.zeros((map_size, map_size), dtype=np.bool_)
        self.open_set = []  # Current frontier
        
    def reset_for_new_target(self, target_x: int, target_y: int):
        """Reset state for new target while preserving exploration"""
        self.closed_set = np.zeros((self.map_size, self.map_size), dtype=np.bool_)
        # First, create a dictionary to track best g_score for each position
        best_positions = {}  # (x,y) -> (g_score, index)
        
        # Find best g_scores and their indices
        for idx, (_, g_score, x, y) in enumerate(self.open_set):
            pos = (x, y)
            if pos not in best_positions or g_score < best_positions[pos][0]:
                best_positions[pos] = (g_score, idx)
        
        # Create new open_set with only the best entries
        new_open_set = []
        for (x, y), (g_score, _) in best_positions.items():
            h_score = UnitDistanceCalculator._manhattan_distance(x, y, target_x, target_y)
            heapq.heappush(new_open_set, (g_score + h_score, g_score, x, y))
            
        self.open_set = new_open_set


class UnitDistanceCalculator:
    def __init__(self, thread_count: int = None):
        self.thread_count = thread_count or mp.cpu_count()
        self.thread_pool = ThreadPoolExecutor(max_workers=self.thread_count)
        
    def __del__(self):
        self.thread_pool.shutdown(wait=True)
        
    @staticmethod
    @jit(nopython=True)
    def _manhattan_distance(x1: int, y1: int, x2: int, y2: int) -> int:
        return abs(x2 - x1) + abs(y2 - y1)
    
    def _a_star_search(self, start_x: int, start_y: int, target_x: int, target_y: int,
                      map_data: np.ndarray, team_matrix: np.ndarray,
                      movement_capacity: int, move_cost_matrix: np.ndarray,
                      exploration_state: UnitExploration) -> int:
        """
        A* pathfinding implementation that maintains state between searches
        """
        if start_x == target_x and start_y == target_y:
            return 0
        
        # If target is already explored and within movement range, return cached distance
        if exploration_state.g_scores[target_x, target_y] <= movement_capacity:
            return exploration_state.g_scores[target_x, target_y]
        
        # Reset state for new target search
        exploration_state.reset_for_new_target(target_x, target_y)
        
        # Initialize start position if not already explored
        if exploration_state.g_scores[start_x, start_y] == np.inf:
            exploration_state.g_scores[start_x, start_y] = 0
            h_score = UnitDistanceCalculator._manhattan_distance(start_x, start_y, target_x, target_y)

            heapq.heappush(exploration_state.open_set, 
                          (h_score, 0, start_x, start_y))

        
        while exploration_state.open_set:
            f_score, g_score, current_x, current_y = heapq.heappop(exploration_state.open_set)

            # If f_score exceeds movement capacity, all remaining paths will too
            if f_score > movement_capacity:
                heapq.heappush(exploration_state.open_set, (f_score, g_score, current_x, current_y))
                return 99
            
            # Skip if we've found a better path to this node
            if g_score > exploration_state.g_scores[current_x, current_y]:
                continue
                
            # Check if we've reached target or are adjacent to enemy
            if (current_x == target_x and current_y == target_y):
                heapq.heappush(exploration_state.open_set, (f_score, g_score, current_x, current_y))
                return g_score
                
            # Mark as visited
            exploration_state.closed_set[current_x, current_y] = True
            
            # Check neighbors
            for dx, dy in [(1,0), (-1,0), (0,1), (0,-1)]:
                next_x, next_y = current_x + dx, current_y + dy
                
                # Check bounds and closed set
                if (next_x < 0 or next_x >= len(map_data) or 
                    next_y < 0 or next_y >= len(map_data) or
                    exploration_state.closed_set[next_x, next_y]):
                    continue
                    
                # Calculate new g_score
                tentative_g = g_score + move_cost_matrix[next_x, next_y]
                
                # Skip if doesn't improve path
                if tentative_g >= exploration_state.g_scores[next_x, next_y]:
                    continue
                
                # Calculate h_score (manhattan distance to target)
                h_score = UnitDistanceCalculator._manhattan_distance(next_x, next_y, target_x, target_y)
                
                # Update g_score and add to open set
                exploration_state.g_scores[next_x, next_y] = tentative_g
                f_score = tentative_g + h_score
                heapq.heappush(exploration_state.open_set, 
                              (f_score, tentative_g, next_x, next_y))
        
        return 99  # No path found within movement capacity

    def process_unit(self, unit_data: Tuple[Any, int], 
                    units: List[Any], map_data: np.ndarray,
                    max_units: int) -> Tuple[np.ndarray, np.ndarray]:
        """
        Process a single unit's distances and access capabilities using A* pathfinding
        """
        unit, index = unit_data
        map_size = len(map_data)
        team_matrix = np.full((map_size, map_size), 0)
        move_cost_matrix = np.array([[unit.move_cost(cell) for cell in row] for row in map_data])
        # enemy units cost 99 to move through
        for u in units:
            if u.team != unit.team:
                move_cost_matrix[u.x, u.y] = 99
        
        for u in units:
            team_matrix[u.x, u.y] = u.team
            
        unobstructed_distances = [99]*max_units
        can_access = [0]*max_units
        
        # Create exploration state for this unit's position
        exploration_state = UnitExploration(map_size)
        
        # For each target unit
        for i, target in enumerate(units):
            # Early termination check using manhattan distance
            min_move_cost = np.min(move_cost_matrix)
            manhattan_dist = UnitDistanceCalculator._manhattan_distance(unit.x, unit.y, target.x, target.y)
            if manhattan_dist > unit.get_movement_capacity()+1:
                continue
                
            if target.team == unit.team:
                # Direct path for allied units
                distance = self._a_star_search(
                    unit.x, unit.y, target.x, target.y,
                    map_data, team_matrix, unit.get_movement_capacity(),
                    move_cost_matrix, exploration_state
                )
                unobstructed_distances[i] = distance
            else:
                # if adjacent, break early
                if manhattan_dist == 1:
                    unobstructed_distances[i] = 0
                    can_access[i] = 1
                    continue
                # Find closest adjacent tile for enemy units
                min_distance = 99
                # Get all valid adjacent tiles and sort by distance to unit
                adjacent_tiles = []
                for dx, dy in [(1,0), (-1,0), (0,1), (0,-1)]:
                    adj_x, adj_y = target.x + dx, target.y + dy
                    if (0 <= adj_x < map_size and 0 <= adj_y < map_size and team_matrix[adj_x, adj_y] == 0):
                        manhattan_dist = self._manhattan_distance(unit.x, unit.y, adj_x, adj_y)
                        adjacent_tiles.append((manhattan_dist, adj_x, adj_y))

                # Sort by manhattan distance
                adjacent_tiles.sort()  # Will sort by manhattan_dist since it's first element

                # Check tiles in order of closest to farthest
                for _, adj_x, adj_y in adjacent_tiles:
                    distance = self._a_star_search(
                        unit.x, unit.y, adj_x, adj_y,
                        map_data, team_matrix, unit.get_movement_capacity(),
                        move_cost_matrix, exploration_state
                    )
                    min_distance = min(min_distance, distance)
                    if min_distance <= unit.get_movement_capacity():
                        can_access[i] = 1
                        break

                unobstructed_distances[i] = min_distance
        # if any unobstructed_distances = 99, set to -1
        for i in range(len(unobstructed_distances)):
            if unobstructed_distances[i] == 99:
                unobstructed_distances[i] = -1

        return unobstructed_distances, can_access



    def create_gcn_input(self, units: List[Any], map_data: List[List[Any]], max_units: int) -> Tuple[List[List[int]], List[List[float]], List[List[float]], List[List[float]], List[List[float]]]:
        """
        Create GCN input for a single request
        """
        # Initialize matrices with zeros using max_units dimensions
        man_distance = [[0 for _ in range(max_units)] for _ in range(max_units)]
        damage = [[0 for _ in range(max_units)] for _ in range(max_units)]
        
        # Fill the matrices up to the actual number of units
        num_units = len(units)
        for i in range(num_units):
            for j in range(num_units):
                # Manhattan distance
                man_distance[i][j] = abs(units[i].x - units[j].x) + abs(units[i].y - units[j].y)
                
                # Damage calculation
                if units[i].team != units[j].team:
                    damage[i][j] = units[i].calc_dmg(units[j], map_data[units[j].x][units[j].y])
        
        # Prepare feature matrix
        feature_matrix = [[0 for _ in range(4)] for _ in range(max_units)]
        for i, unit in enumerate(units):
            feature_matrix[i] = [
                unit.hp,
                unit.type if unit.team == 0 else -1 * unit.type,
                unit.get_movement_capacity(),
                1 if unit.team == 1 or unit.moved else 0
            ]

        # Process units in parallel
        unit_data = [(unit, i) for i, unit in enumerate(units)]
        results = list(self.thread_pool.map(
            lambda x: self.process_unit(x, units, map_data, max_units),
            unit_data
        ))
        
        # Combine results
        unobstructed_distance = [r[0] for r in results]
        can_access = [r[1] for r in results]
        # add blank rows to fill in matrices
        while len(unobstructed_distance) < max_units:
            unobstructed_distance.append([-1] * max_units)
            can_access.append([0] * max_units)
        
        return (can_access, feature_matrix, man_distance, unobstructed_distance, damage)

# Example usage:
"""
# Create calculator once and reuse
calculator = UnitDistanceCalculator(thread_count=8)

# Process single request
result = calculator.create_gcn_input(units, map_data, max_units)

# Process multiple requests
requests = [
    GCNRequest(units1, map_data1, max_units),
    GCNRequest(units2, map_data2, max_units),
]
results = calculator.process_gcn_requests(requests)
"""