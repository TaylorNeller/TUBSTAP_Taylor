import pandas as pd
from MapGen import MapGen
import os
import Preprocess
import sys

# Add the port directory to the Python path
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), 'port')))
import port.GameCLI as GameCLI



class DataManager:
    data_dir = './autobattle/'
    data_csv = './Autobattle20240522.csv'
    csv_dir = './data/'
    buffer_size = 1000
    update_size = 16
    minibatch_size = 16
    verbose = False

    @staticmethod
    # top of buffer are most recent
    def update_replay_buffer(update_size=None):
        if update_size is None:
            if DataManager.verbose:
                print(f'default update size: {DataManager.update_size}')
            update_size = DataManager.update_size

        #  load used indices from sampled_indices.csv, making empty dataframe if there are no columns
        try:
            sampled_indices = pd.read_csv(f'{DataManager.csv_dir}sampled_indices.csv') 
        except:
            sampled_indices = pd.DataFrame()

        # Load the data from replay_buffer.csv into dataframe, making empty dataframe if there are no columns
        try:
            old_data = pd.read_csv(f'{DataManager.csv_dir}replay_buffer.csv')
            old_points = old_data.drop(columns=['Unnamed: 0'], errors='ignore')
            if len(sampled_indices) > 0:
                old_points = old_points.drop(sampled_indices['Index'], axis=0)
        except:
            old_points = pd.DataFrame()    
        
        if update_size > 0:
            DataManager.generate_new_maps(update_size)
            DataManager.annotate_maps(DataManager.data_csv)
            Preprocess.read_all_maps(DataManager.data_dir)
            Preprocess.annotate_battle_results(DataManager.data_csv)
            new_data, new_labels = Preprocess.load_gcn_matrices(DataManager.data_csv)
            # new_data, new_labels = load_dummy_data()

            # print('new_data:', new_data)
            # print('new_labels:', new_labels)
        else:
            new_data = None
            new_labels = None

        if (new_data is None) or (new_labels is None):
            replay_buffer = old_points
        else:
            new_points = pd.DataFrame()
            new_points['Adjacency'] = [data_point[0] for data_point in new_data]
            new_points['Feature'] = [data_point[1] for data_point in new_data]
            new_points['EdgeFeature1'] = [data_point[2][0] for data_point in new_data]
            new_points['EdgeFeature2'] = [data_point[2][1] for data_point in new_data]
            new_points['EdgeFeature3'] = [data_point[2][2] for data_point in new_data]
            new_points['Label'] = new_labels

            # append new_data to old_data
            replay_buffer = pd.concat([new_points, old_points], ignore_index=True)
            # remove oldest data if buffer is full (points towards end of dataframe are oldest)
            if len(replay_buffer) > DataManager.buffer_size:
                replay_buffer = replay_buffer.head(DataManager.buffer_size)

        # save replay_buffer to replay_buffer.csv
        replay_buffer.to_csv(f'{DataManager.csv_dir}replay_buffer.csv')

        # clear sampled_points.csv
        sampled_indices = pd.DataFrame()
        sampled_indices.to_csv(f'{DataManager.csv_dir}sampled_indices.csv')

        if DataManager.verbose:
            print(f'updated replay buffer with {update_size} new maps in {DataManager.data_dir} and saved to replay_buffer.csv')

    @staticmethod
    def generate_new_maps(n):
        if DataManager.verbose:
            print(f'generated {n} new maps in {DataManager.data_dir}')
        
        # clear all old maps from data_dir
        for file in os.listdir(DataManager.data_dir):
            if file.startswith('genmap'):
                os.remove(DataManager.data_dir + file)

        for i in range(n):
            MapGen.generate_map_file(f"{DataManager.data_dir}genmap{i}.tbsmap", 8, 8)

    @staticmethod
    def annotate_maps(csv_loc):
        if DataManager.verbose:
            print(f'annotating maps in {DataManager.data_dir}...')

        # delete old csv file
        if os.path.exists(csv_loc):
            os.remove(csv_loc)

        GameCLI.run_autobattle(user1=0, user2=0, map="AutoBattles", output=csv_loc, games=2)
        if DataManager.verbose:
            print(f'annotated maps in {DataManager.data_dir} and saved to {csv_loc}')

    @staticmethod
    def get_sampled_minibatch(minibatch_size=None, delete_sampled_indices=True):
        if minibatch_size is None:
            minibatch_size = DataManager.minibatch_size
        # Load the data from replay_buffer.csv into dataframe
        replay_buffer = pd.read_csv(f'{DataManager.csv_dir}replay_buffer.csv')

        # sample minibatch_size indices from replay_buffer
        # sampled_points: points sampled
        # sampled_indices: indices of sampled points
        sampled_points = replay_buffer.sample(minibatch_size)
        sampled_indices = sampled_points.index
        sampled_indices = pd.DataFrame(sampled_indices, columns=['Index'])

        sampled_points = sampled_points.drop(columns=['Unnamed: 0'], errors='ignore')
        sampled_data = sampled_points.drop(columns=['Label'])
        label_column = sampled_points['Label']

        
        # save sampled_indices (type Index) to sampled_indices.csv
        if delete_sampled_indices:
            sampled_indices.to_csv(f'{DataManager.csv_dir}sampled_indices.csv')


        if DataManager.verbose:
            print(f'sampled {minibatch_size} points from replay buffer')

        return sampled_data, label_column


# main
if __name__ == "__main__":
    DataManager.update_replay_buffer()
    data, labels = DataManager.get_sampled_minibatch()
    # print('data: ', data)
    # print('labels: ', labels)
    # print('data shape:', data.shape)
    # print('labels shape:', labels.shape)