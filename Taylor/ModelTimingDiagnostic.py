import time
import tensorflow as tf
from MapUtils import MapUtils

class ModelTimingDiagnostic:
    def __init__(self, model):
        self.model = model
        self.timing_stats = {
            'data_prep': [],
            'tensorization': [],
            'inference': [],
            'total': []
        }
        
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
            tf.keras.backend.clear_session()
        
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


    def get_valid_actions_for_state(self, map_, team_color):
        """Get all valid actions for the current state."""
        valid_actions = []
        units = map_.get_units_list(team_color, False, True, False)
        
        for unit in units:
            unit_actions = MTools.get_unit_actions(unit, map_)
            valid_actions.extend(unit_actions)
        
        return valid_actions