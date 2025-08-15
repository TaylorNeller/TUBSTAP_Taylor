from MTools import MTools
from Player import Player
import time
from MapUtils import MapUtils
import numpy as np
from UnitData import *
# from NNModel import NNModel
from GCNModel import GCNModel
from GCNBasicModel import GCNBasicModel
from CNNModel import CNNModel
from CNNGraphModel import CNNGraphModel
from CombinedCNNModel import CombinedCNNModel
from CombinedModel import CombinedModel


import tensorflow as tf

class AI_Minimax(Player):
    # Parameters
    MAX_DEPTH = 2  # Fixed depth for minimax search
    MAX_UNITS = 6
    FAST_MATRIX_FORMAT = True
    MASK_ADJACENCY = False

    def __init__(self):
        self.stopwatch = time.time()
        # self.model = GCNModel("plains_unit-list/GCN_fast_b128_e30.keras")
        # self.model = GCNModel("plains_unit-list/GCN_fast_b128_e50.keras")
        self.model = GCNModel("plains_unit-list/GCN_fast_b128_e75.keras")
        # self.model = GCNModel("plains_unit-list/GCN_fast_b128_e100.keras")
        # self.model = CNNModel("plains_unit-list/CNNi_fast_b128_e30.keras")
        # self.model = CNNModel("plains_unit-list/CNNi_fast_b128_e50.keras")
        # self.model = CNNGraphModel("plains_unit-list/CNNg_fast_b32_e50.keras")
        # self.model = CombinedCNNModel("plains_unit-list/CNNc_fast_b32_e50.keras")
        # self.model = CombinedModel("plains_unit-list/CGNNc_fast_b32_e50.keras")
        # self.model = GCNBasicModel("plains_unit-list/GCNBasic_M_b128_e20.keras")
        # self.model = GCNBasicModel("plains_unit-list/GCNBasic_slowM_b128_e50.keras")
        self.model.load_model()
        # self.timing_stats = {
        #     'data_prep': [],
        #     'tensorization': [],
        #     'inference': [],
        #     'total': []
        # }

    def get_name(self):
        return "Minimax-D2"

    def show_parameters(self):
        return ""

    def make_action(self, map_, team_color, turn_start, game_start):
        # print('Starting minimax decision process')
        
        # run diagnostic batch
        # self.run_diagnostic_batch(map_, team_color, 100)
        # self.print_stats()
        # raise Exception("Diagnostic batch complete")


        # Get the best sequence of moves
        if self.MAX_DEPTH == 1:
            best_sequence = self.depth1minimax(map_, team_color)
        else:
            best_sequence = self.depth2minimax(map_, team_color)
        
        # Return the first action from the best sequence
        return best_sequence
    
    def depth1minimax(self, map_, team_color):
        # Get all valid actions for the current state
        valid_actions = self.get_valid_actions_for_state(map_, team_color)
        states = []
        
        for action in valid_actions:
            # Create new state after first action
            state = map_.create_deep_clone()
            state.execute_action(action)
            states.append((action, state))

        # states labeled in range [-1,1] 
        evals = self.batch_eval(states)
        if team_color == 1:
            evals = [-1 * x for x in evals] # invert evals for blue team

        # choose best action
        return valid_actions[np.argmax(evals)]

    def depth2minimax(self, map_, team_color):
        # Start with first unit's possible actions
        first_actions = self.get_valid_actions_for_state(map_, team_color)
        final_states = []
        
        for first_action in first_actions:
            # Create new state after first action
            first_state = map_.create_deep_clone()
            first_state.execute_action(first_action)
            
            # Get valid actions for remaining units in the new state
            second_actions = self.get_valid_actions_for_state(first_state, team_color)

            if len(second_actions) == 0:
                final_states.append((first_action, first_state))

            # Try each possible second action
            for second_action in second_actions:
                # Create final state after both actions
                final_state = first_state.create_deep_clone()
                final_state.execute_action(second_action)
                final_states.append((first_action, final_state))
        
        # states labeled in range [-1,1] 
        evals = self.batch_eval(final_states)
        if team_color == 1:
            evals = [-1 * x for x in evals] # invert evals for blue team
        
        # create list of evals for each first action, choosing the lowest one for each first action
        best_evals = [999]
        curr_state = final_states[0][0]
        for i in range(len(final_states)):
            if final_states[i][0] != curr_state:
                best_evals.append(999)
                curr_state = final_states[i][0]
            if evals[i] < best_evals[len(best_evals)-1]:
                best_evals[len(best_evals)-1] = evals[i]

        # print('calculated turn')

        # find the best first action
        return first_actions[np.argmax(best_evals)]
    

    
    def batch_eval(self, final_states):        
        # print('batch_eval start')
        # start_time = time.time()

        data = [[],[],[],[],[],[],[],[],[],[]]

        for state in final_states:
            map_matrix, unit_list, move = self.to_matrix_ulist(state[1], state[0])

            new_mats = MapUtils.create_data_matrices(unit_list, map_matrix, self.MAX_UNITS, self.FAST_MATRIX_FORMAT, self.MASK_ADJACENCY) 
            for i in range(len(new_mats)):
                data[i].append(new_mats[i])

        # print('data extracted. Time taken to create data matrices: ')


        tensors = self.model.tensorize_data(data)
        input = self.model.select_data(tensors)

        # print('data tensorized')
        # print([input[i].shape for i in range(len(input))])

        continuous_preds, pred_labels = self.model.perform_inference(input)
        # print('batch_eval end. Time taken to perform inference: ', time.time() - start_time)
        return continuous_preds
        

    def to_matrix_ulist(self, map, move):
        # clone map matrix (map.map_field_type)
        map_matrix = []
        for row in map.map_field_type:
            map_matrix.append(row.copy())
        # create UnitData objects from units in map
        unit_list = []
        for unit in map.units:
            if unit is not None:
                unit_str = unit.spec.get_spec_name()
                type = ConstsData.unitNames.index(unit_str)
                moved = 1 if unit.action_finished else 0
                unit_list.append(UnitData(unit.x_pos, unit.y_pos, type, unit.team_color, unit.HP, moved))
        return (map_matrix, unit_list, str(move))
    
    

    def run_diagnostic_batch(self, map_, team_color, n_iterations=5):
        """Run multiple batches and collect timing statistics."""
        print("Running timing diagnostics...")
        
        for i in range(n_iterations):
            start_total = time.perf_counter()
            
            # Time data preparation
            t0 = time.perf_counter()
            valid_actions = self.get_valid_actions_for_state(map_, team_color)
            states = []
            for action in valid_actions:
                state = map_.create_deep_clone()
                state.execute_action(action)
                states.append((action, state))
            t1 = time.perf_counter()
            self.timing_stats['data_prep'].append(t1 - t0)
            
            # Time tensorization
            t0 = time.perf_counter()
            data = [[], [], [], [], [], [], [], [], [], []]
            for state in states:
                map_matrix, unit_list, move = self.to_matrix_ulist(state[1], state[0])
                new_mats = MapUtils.create_data_matrices(unit_list, map_matrix, 6, True)
                for i in range(len(new_mats)):
                    data[i].append(new_mats[i])
            tensors = self.model.tensorize_data(data)
            input = self.model.select_data(tensors)
            t1 = time.perf_counter()
            self.timing_stats['tensorization'].append(t1 - t0)
            
            # Time inference
            t0 = time.perf_counter()
            continuous_preds, pred_labels = self.model.perform_inference(input)
            t1 = time.perf_counter()
            self.timing_stats['inference'].append(t1 - t0)
            
            self.timing_stats['total'].append(time.perf_counter() - start_total)
            
            # Force TensorFlow to execute immediately
            # tf.keras.backend.clear_session()
        
    def print_stats(self):
        """Print timing statistics."""
        print("\nTiming Statistics (in milliseconds):")
        print("-" * 50)
        for key, times in self.timing_stats.items():
            avg = sum(times) / len(times) * 1000  # Convert to ms
            min_time = min(times) * 1000
            max_time = max(times) * 1000
            print(f"{key.replace('_', ' ').title()}:")
            print(f"  Avg: {avg:.2f}ms")
            print(f"  Min: {min_time:.2f}ms")
            print(f"  Max: {max_time:.2f}ms")
            print()
