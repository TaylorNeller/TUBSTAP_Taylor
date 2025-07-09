from Trainer import Trainer
from DataLoader import DataLoader
from MapGen import MapGen
from MapUtils import MapUtils
from UnitDistanceCalculator import UnitDistanceCalculator
import numpy as np
import sys
import os
import time
from collections import Counter

# Add the port directory to the Python path
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), 'port')))
import port.GameCLI as GameCLI

def runGames():
    start_time = time.time()

    results = []
    games_per_side = 10
    players = [3, 2]
    for i in range(games_per_side):
        map = MapGen.generate_map_file()
        # print(map)
        _, result = GameCLI.run_autobattle(user1=players[0], user2=players[1], map='random_map', games=2, raw_map_str=map)
        results.append(result)
    for i in range(games_per_side):
        map = MapGen.generate_map_file()
        # print(map)
        _, result = GameCLI.run_autobattle(user1=players[1], user2=players[0], map='random_map', games=2, raw_map_str=map)
        results.append(-result)
    print(results)
    print(np.mean(results))
    print('Time taken to run games: ', time.time() - start_time)

def process_raw_Cs_csv():
    data, labels = dl.load_raw_Cs_output('raw_Cs_output.csv')
    n_eval = int(len(labels)*.1)

    data_tail = data[-10000:]
    labels_tail = labels[-10000:]
    n_neg = int(labels_tail.count(-1)*.1)
    n_pos = int(labels_tail.count(1)*.1)
    new_data = []
    new_labels = []
    for i in range(len(labels_tail)):
        if labels_tail[i] == -1:
            if n_neg > 0:
                new_data.append(data_tail[i])
                new_labels.append(-1)
                n_neg -= 1
        elif labels_tail[i] == 1:
            if n_pos > 0:
                new_data.append(data_tail[i])
                new_labels.append(1)
                n_pos -= 1
        else:
            new_data.append(data_tail[i])
            new_labels.append(0)
    data_tail = new_data
    labels_tail = new_labels

    data_head = data[:-10000]
    labels_head = labels[:-10000]
    n_neg = int(labels_head.count(-1)*.1)
    n_pos = int(labels_head.count(1)*.1)
    new_data = []
    new_labels = []
    for i in range(len(labels_head)):
        if labels_head[i] == -1:
            if n_neg > 0:
                new_data.append(data_head[i])
                new_labels.append(-1)
                n_neg -= 1
        elif labels_head[i] == 1:
            if n_pos > 0:
                new_data.append(data_head[i])
                new_labels.append(1)
                n_pos -= 1
        else:
            new_data.append(data_head[i])
            new_labels.append(0)
    data_head = new_data
    labels_head = new_labels

    print(Counter(labels_head))
    print(Counter(labels_tail))

    data = dl.transpose(data_head)
    labels = labels_head
    dl.save_data_cols(data, labels, 'MCTS_train_large-faster.csv')
    data_tensors, label_tensors = dl.tensorize_data(data, labels)
    dl.save_tensors(data_tensors, label_tensors, 'MCTS_train_large-faster')
    print('Training set: ', len(labels))

    data = dl.transpose(data_tail)
    labels = labels_tail
    dl.save_data_cols(data, labels, 'MCTS_eval_large-faster.csv')
    data_tensors, label_tensors = dl.tensorize_data(data, labels)
    dl.save_tensors(data_tensors, label_tensors, 'MCTS_eval_large-faster')
    print('Test set: ', len(labels))

def train_model():
    # trainer = Trainer('CNN_M_b32_e30.keras', model_type="CNN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=True)
    trainer = Trainer('GCN_M_b128_e20.keras', model_type="GCN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=True)
    trainer.train("MCTS_train_large-faster", batch_size=128, epochs=20)
    trainer.eval("MCTS_eval_large-faster")
    # trainer.train("MCTS_train_large-fast", batch_size=128, epochs=20)
    # trainer.eval("MCTS_eval_large-fast")

if __name__ == "__main__":
    print("running job")
    
    dl = DataLoader('./models/plains_unit-list/')
    dl.setup_workers(1)

    # process_raw_Cs_csv()
    # train_model()
    runGames()

    # t_data, t_labels = dl.load_tensors('good_data_tensors')
    # data, labels = dl.detensorize_data(t_data, t_labels)
    # dl.save_data_cols(data, labels, 'good_data2.csv')
    # data, labels = dl.load_dataframe('good_eval.csv')
    # data_tensors, label_tensors = dl.tensorize_data(data, labels)
    # dl.save_tensors(data_tensors, label_tensors, 'good_eval')

    # data, labels = dl.load_dataframe('100_100_100_data.csv')
    # calculator = UnitDistanceCalculator()

    # # for index in range(50):
    # #     map, units = dl.data_to_map_units(data, index)
    # #     data1 = MapUtils.create_gcn_input_new(units, map, 6)
    # #     data2 = calculator.create_gcn_input(units, map, 6)

    # #     # if the can_access matrices are different, print index
    # #     if not np.array_equal(data1[0], data2[0]):
    # #         print(index)

    # map, units = dl.data_to_map_units(data, 13)
    # print(MapUtils.map_to_string(map, units))
    # for unit in units:
    #     print(unit)

    # data = MapUtils.create_gcn_input_new(units, map, 6)
    # # convert each of the matrices in data to np matrices
    # data1 = [np.array(data[i]) for i in range(len(data))]
    # data2 = calculator.create_gcn_input(units, map, 6)

    # print(data1)
    # print(data2)

    # dl.gen_and_save_data_WLD(34863, 19768, 28429)
    # dl.gen_and_save_data_WLD(1783, 1186, 890)
    # dl.gen_and_save_data_WLD(10, 10, 10, 'ai')
    # dl.gen_and_save_data_points(5000, 'ai')
    # dl.gen_and_save_data_WLD(100, 100, 100)
    # dl.gen_and_save_data_WLD(1000, 1000, 1000)
    # dl.gen_and_save_data_WLD(10000, 10000, 10000)
    # print('Time taken to generate and save data: ', time.time() - start_time)

    # dl.test_map_balance(20, 3)


    

    # data, labels = dl.load_df_rows('10_10_10_fast.csv')
    # print(MapUtils.map_str_from_data(data[0]))

    # game_results = GameCLI.run_battles(user1=0, user2=0, data_loader=dl)
    # print(game_results)


    # trainer = Trainer('IGNN5.keras', model_type="ImprovedGNN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer = Trainer('GCN2-M20.keras', model_type="GCN2", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer = Trainer('MPGNN_M20.keras', model_type="MPGNN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer = Trainer('CombinedCNN-M20.keras', model_type="CombinedCNN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer = Trainer('CNNGraph30fast.keras', model_type="CNNGraph", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=True)
    # trainer = Trainer('test_model_30b.keras', model_type="GCN", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer = Trainer('combined20.keras', model_type="Combined", active_dir='./models/plains_unit-list/', keep_better=False, delete_old=False)
    # trainer.update_training_log('')
    # DataManager.test_map_balance(100)

    # trainer.train("100_100_100_data", batch_size=64, epochs=5)
    # trainer.train("976_1467_2593_ai", batch_size=64, epochs=30)
    # trainer.eval("1783_1186_890_data")
    # trainer.train("10000_10000_10000_data.csv", batch_size=64, epochs=10)
    # trainer.train("good_data_tensors", batch_size=64, epochs=5)
    # trainer.train("good_data2.csv", batch_size=64, epochs=10)
    # trainer.eval("good_eval")
    # for i in range(5):
    #     trainer.train(1000, n_epochs=100)
    # trainer.train_with_lr_decay(Params.loops_per_lr, Params.num_cycles, Params.learning_rate, Params.decay_rate)

    # cnn20 = .80
    # gcn20 = .72
    # combined = .79

