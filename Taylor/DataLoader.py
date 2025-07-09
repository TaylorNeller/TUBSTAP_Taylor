import pandas as pd
from MapGen import MapGen
import os
import sys
import ast
from joblib import Parallel, delayed
import time
from MapUtils import MapUtils
from multiprocessing import Manager, Pool
import random
import math
import numpy as np
from UnitData import *
import tensorflow as tf

# Add the port directory to the Python path
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), 'port')))
import port.GameCLI as GameCLI

ai_collector = 2

def collect_game_states_worker(args):
    """Standalone worker function for collecting game states"""
    trim_data, points_per_game, max_units = args
    map_str = MapGen.generate_map_file()
    states, result = GameCLI.run_autobattle(
        user1=ai_collector, user2=ai_collector, 
        map='random_map', 
        games=2, 
        raw_map_str=map_str
    )

    if len(states) < points_per_game:
        print('DataManager: GAME GENERATED LESS POINTS THAN points_per_game')
        return collect_game_states_worker((trim_data, points_per_game))

    if trim_data:
        states = random.sample(states, points_per_game)

    data = []
    labels = []
    for state in states:
        map_matrix, unit_list, move = state
        new_data = MapUtils.create_data_matrices(unit_list, map_matrix, max_units)
        data.append(new_data)
        labels.append(result)

    return data, labels


class DataLoader:

    def __init__(self, dir=None):
        self.active_dir = "./" if dir is None else dir
        
        self.buffer_size = 100000
        self.n_workers = 4 # num games per update using parallel processing
        self.minibatch_size = 64
        self.points_per_game = 4
        
        self.num_nodes = 6

        self.verbose = False
        self.full_parallel = True
        self.pool = None

        self.replay_buffer = None
        self.saved_replay_buffer = None
        self.saving_sampled = False

    def collect_games(self, n_games=None, trim_data=True):
        """Collect multiple games in parallel"""
        if n_games is None:
            n_games = self.n_workers

        if self.pool is None:
            print("Workers not initialized. Calling setup_workers()")
            self.setup_workers()

        if n_games > self.pool._processes:
            print('DataManager: WARNING: n_games > number of workers')

        try:

            if n_games == 1:
                # Use the standalone worker function
                return collect_game_states_worker((trim_data, self.points_per_game, self.num_nodes))
                
            # Create arguments list with trim_data and points_per_game
            args = [(trim_data, self.points_per_game, self.num_nodes) for _ in range(n_games)]
            
            # Use the standalone worker function
            games = self.pool.map(collect_game_states_worker, args)
            
            new_data, new_labels = [], []
            for game in games:
                game_data, game_labels = game
                new_data.extend(game_data)
                new_labels.extend(game_labels)
                
            return new_data, new_labels
            
        except Exception as e:
            print(f"Error in collect_games: {e}")
            self.pool.close()
            self.pool.join()
            self.pool = None
            raise

    def setup_workers(self, n_workers=None):
        """Initialize the worker pool"""
        if self.pool is not None:
            self.pool.close()
            self.pool.join()

        if n_workers is None:
            n_workers = self.n_workers
        else:
            self.n_workers = n_workers

        def init_worker():
            # Set a random seed for each worker
            seed = random.randint(0, 2**32 - 1)
            random.seed(seed)
            np.random.seed(seed)
                  
        self.pool = Pool(
            processes=n_workers,
            initializer=init_worker
        )

    def gen_data_WLD(self, win_n, loss_n, draw_n):  
        total_n = win_n + loss_n + draw_n
        win_c, loss_c, draw_c = 0, 0, 0
        data, labels = [], []

        c = 0
        while len(data) < total_n:
            new_data, new_labels = self.collect_games(self.n_workers, False) # Record whole games
            for i in range(len(new_labels)):
                if new_labels[i] == 1 and win_c < win_n:
                    data.append(new_data[i])
                    labels.append(new_labels[i])
                    win_c += 1
                elif new_labels[i] == -1 and loss_c < loss_n:
                    data.append(new_data[i])
                    labels.append(new_labels[i])
                    loss_c += 1
                elif new_labels[i] == 0 and draw_c < draw_n:
                    data.append(new_data[i])
                    labels.append(new_labels[i])
                    draw_c += 1
            if c % 100 == 9:
                print('w:', win_c, ' l:', loss_c, ' d:', draw_c)
            c += 1
        
        return data, labels

    def gen_data_points(self, n_points):  
        win_c, loss_c, draw_c = 0, 0, 0
        data, labels = [], []

        c = 0
        while len(data) < n_points:
            new_data, new_labels = self.collect_games(self.n_workers, False) # Record whole games
            for i in range(len(new_labels)):
                data.append(new_data[i])
                labels.append(new_labels[i])
                if new_labels[i] == 1:
                    win_c += 1
                elif new_labels[i] == -1:
                    loss_c += 1
                elif new_labels[i] == 0:
                    draw_c += 1
            if c % 100 == 9:
                print('w:', win_c, ' l:', loss_c, ' d:', draw_c)
            c += 1
        
        return data, labels, [win_c, loss_c, draw_c]
    
    def save_data(self, data, labels, fname):
        data_df = self.data_row_to_df(data, labels)

        # save replay_buffer to replay_buffer.csv
        data_df.to_csv(self.active_dir+fname)

        print(f'Saved {len(labels)} maps in {self.active_dir} and saved to {fname}')

    def save_data_cols(self, data, labels, fname):
        data_df = self.data_col_to_df(data, labels)

        # save replay_buffer to replay_buffer.csv
        data_df.to_csv(self.active_dir+fname)

        print(f'Saved {len(labels)} maps in {self.active_dir} and saved to {fname}')

    def gen_and_save_data_WLD(self, win_n, loss_n, draw_n, data_format='fast'):
        # data_format = 'fast'
        # data_format = 'mcts'

        data, labels = self.gen_data_WLD(win_n, loss_n, draw_n)
        csv_fname = f'{win_n}_{loss_n}_{draw_n}_{data_format}.csv'
        self.save_data(data, labels, csv_fname)

        tensor_fname = f'{win_n}_{loss_n}_{draw_n}_{data_format}'
        col_data = self.transpose(data)
        data_tensors, label_tensors = self.tensorize_data(col_data, labels)
        self.save_tensors(data_tensors, label_tensors, tensor_fname)

        # create txt file with the same name as the csv file
        with open(self.active_dir+f'{win_n}_{loss_n}_{draw_n}_{data_format}.txt', 'w') as f:
            # write map generation details
            f.write(f'map x, y: {MapGen.map_x}, {MapGen.map_y}\n')
            f.write(f'padded: {MapGen.map_padded}\n')
            f.write(f'local search: {MapGen.local_search_flag}\n')

    def gen_and_save_data_points(self, n_points, data_format='fast'):
        data, labels, wld = self.gen_data_points(n_points)
        csv_fname = f'{wld[0]}_{wld[1]}_{wld[2]}_{data_format}.csv'
        self.save_data(data, labels, csv_fname)

        tensor_fname = f'{wld[0]}_{wld[1]}_{wld[2]}_{data_format}'
        col_data = self.transpose(data)
        data_tensors, label_tensors = self.tensorize_data(col_data, labels)
        self.save_tensors(data_tensors, label_tensors, tensor_fname)

        # create txt file with the same name as the csv file
        with open(self.active_dir+f'{wld[0]}_{wld[1]}_{wld[2]}_{data_format}.txt', 'w') as f:
            # write map generation details
            f.write(f'map x, y: {MapGen.map_x}, {MapGen.map_y}\n')
            f.write(f'padded: {MapGen.map_padded}\n')
            f.write(f'local search: {MapGen.local_search_flag}\n')

    def transpose(self, data):
        result = [[] for _ in range(len(data[0]))]
    
        # Reorganize the data
        for i in range(len(data)):
            for j in range(len(data[0])):
                result[j].append(data[i][j])
        return result

    def load_dataframe(self, fname):
        data_df = pd.read_csv(self.active_dir+fname)
        data_df = data_df.drop(columns=['Unnamed: 0'], errors='ignore')

        data, labels = self.df_to_data(data_df)
        return data, labels
    
    def load_df_rows(self, fname):
        data_df = pd.read_csv(self.active_dir+fname)
        data_df = data_df.drop(columns=['Unnamed: 0'], errors='ignore')

        data, labels = self.df_to_data(data_df)
        data = self.transpose(data)

        return data, labels
    
    def load_raw_Cs_output(self, fname, max_units=6):
        # read csv file, delineator :
        raw_df = pd.read_csv(self.active_dir+fname, sep=':')

        data = []
        labels = []
        for index, row in raw_df.iterrows():
            # get map_matrix from raw string in col 'map'
            map_matrix = ast.literal_eval(row['map'])
            unit_matrix = ast.literal_eval(row['unitlist'])
            unit_list = []
            for unit in unit_matrix:
                unit_list.append(UnitData(unit[0], unit[1], unit[2], unit[3], unit[4], unit[5]))
            result = row['result']

            # map_matrix, unit_list, move = state
            new_data = MapUtils.create_data_matrices(unit_list, map_matrix, max_units)
            data.append(new_data)
            labels.append(result)
        return data, labels


    # df to data, labels
    def df_to_data(self, df):
        adjacency_matrices = []
        feature_matrices = []
        manhattan_matrices = []
        unobstructed_matrices = []
        damage_matrices = []
        map_matrices = []
        type_matrices = []
        team_matrices = []
        hp_matrices = []
        moved_matrices = []
        
        labels = []
        
        for _, data_point in df.iterrows():
            adjacency_matrices.append(np.array(eval(data_point['Adjacency'])))
            feature_matrices.append(np.array(eval(data_point['Feature'])))
            manhattan_matrices.append(np.array(eval(data_point['Manhattan'])))
            unobstructed_matrices.append(np.array(eval(data_point['Unobstructed'])))
            damage_matrices.append(np.array(eval(data_point['Damage'])))
            # manhattan_matrices.append(np.array(eval(data_point['EdgeFeature1'])))
            # unobstructed_matrices.append(np.array(eval(data_point['EdgeFeature2'])))
            # damage_matrices.append(np.array(eval(data_point['EdgeFeature3'])))
        
            map_matrices.append(np.array(eval(data_point['Map'])))
            type_matrices.append(np.array(eval(data_point['Type'])))
            team_matrices.append(np.array(eval(data_point['Team'])))
            hp_matrices.append(np.array(eval(data_point['HP'])))
            moved_matrices.append(np.array(eval(data_point['Moved'])))
                                  
            labels.append(data_point['Label'])

        return [adjacency_matrices, feature_matrices, manhattan_matrices, unobstructed_matrices, damage_matrices, map_matrices, type_matrices, team_matrices, hp_matrices, moved_matrices], labels

    #  takes data, labels in row style
    def data_row_to_df(self, data, labels):
        data_df = pd.DataFrame()
        # GCN
        data_df['Adjacency'] = [data_point[0] for data_point in data]
        data_df['Feature'] = [data_point[1] for data_point in data]
        data_df['Manhattan'] = [data_point[2] for data_point in data]
        data_df['Unobstructed'] = [data_point[3] for data_point in data]    
        data_df['Damage'] = [data_point[4] for data_point in data]
        # CNN
        data_df['Map'] = [data_point[5] for data_point in data]
        data_df['Type'] = [data_point[6] for data_point in data]
        data_df['Team'] = [data_point[7] for data_point in data]
        data_df['HP'] = [data_point[8] for data_point in data]
        data_df['Moved'] = [data_point[9] for data_point in data]

        data_df['Label'] = labels

        return data_df

    #  takes data, labels in row style
    def data_col_to_df(self, data, labels):
        data_df = pd.DataFrame()
        # GCN
        data_df['Adjacency'] = data[0]
        data_df['Feature'] = data[1]
        data_df['Manhattan'] = data[2]
        data_df['Unobstructed'] = data[3]
        data_df['Damage'] = data[4]
        # CNN
        data_df['Map'] = data[5]
        data_df['Type'] = data[6]
        data_df['Team'] = data[7]
        data_df['HP'] = data[8]
        data_df['Moved'] = data[9]

        data_df['Label'] = labels

        return data_df
    
    def data_to_map_units(self, data, index):
        # unpack cnn encoded map layers as map matrix and unit list
        map_matrix = data[5][index]
        type_matrix = data[6][index]
        team_matrix = data[7][index]
        hp_matrix = data[8][index]
        moved_matrix = data[9][index]
        unit_list = []
        for y in range(len(map_matrix)):
            for x in range(len(map_matrix[0])):
                if type_matrix[y][x] != 0:
                    unit_list.append(UnitData(x, y, type_matrix[y][x], team_matrix[y][x], hp_matrix[y][x], moved_matrix[y][x]))
        return map_matrix, unit_list
    
    # takes col style data=[adjacency, feature, mahattan, ...], labels
    def tensorize_data(self, data, labels=None):
        data_tensors = [tf.convert_to_tensor(col, dtype=tf.float32) for col in data]
        if labels is None:
            return data_tensors
        labels = tf.convert_to_tensor(labels, dtype=tf.float32)
        return data_tensors, labels
    
    def detensorize_data(self, data_tensors, label_tensors):
        data_cols = [tf.cast(col, tf.int32).numpy().tolist() for col in data_tensors]
        labels_col = label_tensors.numpy().tolist()
        return data_cols, labels_col

    def save_tensors(self, data_tensors, label_tensors, filename):
        tensor_list = data_tensors + [label_tensors]
        # simple model that contains our tensors
        class TensorContainer(tf.Module):
            def __init__(self, tensors):
                super().__init__()
                self.tensors = [tf.Variable(tensor) for tensor in tensors]
        
        container = TensorContainer(tensor_list)
        tf.saved_model.save(container, self.active_dir+filename)

    def load_tensors(self, filename):
        loaded = tf.saved_model.load(self.active_dir+filename)
        tensors = [tf.convert_to_tensor(var) for var in loaded.tensors]
        return tensors[:-1], tensors[-1]


    def test_map_balance(self, n, player_id=0):
        #  run one game on each map, get the labels, and print distribution of results (%)
        results = []
        for i in range(n):
            map_str = MapGen.generate_map_file()
            states, result = GameCLI.run_autobattle(user1=player_id, user2=player_id, map='random_map', games=2, raw_map_str=map_str)
            results.append(result)

        red_wins = sum([1 for result in results if result == 1])
        blue_wins = sum([1 for result in results if result == -1])
        draws = sum([1 for result in results if result == 0])
        # print(f'balance constant')
        print(f'red wins: {red_wins/n*100}%')
        print(f'blue wins: {blue_wins/n*100}%')
        print(f'draws: {draws/n*100}%')

    def __del__(self):
        """Cleanup method to ensure proper pool shutdown"""
        if self.pool is not None:
            self.pool.close()
            self.pool.join()