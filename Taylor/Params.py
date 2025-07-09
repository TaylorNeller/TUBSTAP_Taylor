from MapGen import MapGen
from BasicGCN2 import BasicGCN
from DataManager import DataManager
import os
from multiprocessing import Manager, Pool
import random

class Params():
    # layers gcn 16, gcn 4, dense 144, dense 512
    # in test data in paper: around 76% data was W/L, 24% was draw
    # starting rate 0.001
    learning_rate = 0.001
    decay_rate = .98
    loops_per_lr = 500
    lr_type = 'exponential'
    num_cycles = 7
    
    gcn_layers = [16, 32, 64]
    dense_layers = [128, 64]
    # gcn_layers = [16, 4]
    # dense_layers = [144, 512]

    model_dir = "./models/"
    model_type = "gcn"
    training_file = "34863_19768_28429_data.csv"
    # training_file = "1000_1000_1000_data.csv"
    eval_file = "1783_1186_890_data.csv"
    # eval_file = "100_100_100_data.csv"

    # data
    buffer_size = 100000
    update_size = 4 # number of games to play in update
    minibatch_size = 64
    points_per_game = 4
    use_disk = False
    data_verbose = False

    # map
    terrain_type = "plains"
    unit_dist = "unit-list"
    unit_list = ["infantry", "infantry", "panzer", "infantry", "infantry", "panzer"]
    local_search_flag = True
    fta_counter = 1.06
    n_red = 3
    n_blue = 3
    seed = -1
    map_x = 8
    map_y = 8
    map_padded = False
    turn_limit = 12


    # model
    num_nodes = 6
    num_node_features = 4
    num_edge_features = 3 
    num_cnn_inputs = 5
    metric = "mae"
    epsilon = 0.5
    symmetrize = True

    print_interval = 100

    @staticmethod
    def worker_task(state, seed):
        Params.load_params(state, seed)

    @staticmethod
    def setup_workers(n_workers=None):
        state = Params.capture_params_state()
        Params.load_params(state)
        if n_workers is None:
            n_workers = DataManager.update_size
        elif n_workers != DataManager.update_size:
            print(f'WARNING: number of workers ({n_workers}) is different from update size ({DataManager.update_size})')

        seeds = [random.randint(0, 2**32 - 1) for _ in range(n_workers)]
        worker_args = [(state, seed) for seed in seeds]
        DataManager.pool = Pool(processes=n_workers)
        DataManager.pool.starmap(Params.worker_task, worker_args)

    @staticmethod
    def capture_params_state():
        return {attr: getattr(Params, attr) for attr in dir(Params) if not callable(getattr(Params, attr)) and not attr.startswith("__")}

    @staticmethod
    def load_params(state=None, seed=None):
        if state is not None:
            for key, value in state.items():
                setattr(Params, key, value)
        if seed is not None:
            random.seed(seed)

        active_dir = f'{Params.model_dir}{Params.model_type}_{Params.terrain_type}_{Params.unit_dist}/'

        # setup map gen
        MapGen.terrain_type = Params.terrain_type
        MapGen.unit_dist = Params.unit_dist
        MapGen.n_red = Params.n_red
        MapGen.n_blue = Params.n_blue
        MapGen.seed = Params.seed
        MapGen.fta_counter = Params.fta_counter
        MapGen.turn_limit = Params.turn_limit
        MapGen.local_search_flag = Params.local_search_flag
        MapGen.unit_list = Params.unit_list
        MapGen.map_x = Params.map_x
        MapGen.map_y = Params.map_y
        MapGen.map_padded = Params.map_padded

        if MapGen.seed != -1:
            print('ALL MAPS IDENTICAL WITH SEED ', MapGen.seed)

        # setup data manager
        DataManager.active_dir = active_dir
        DataManager.buffer_size = Params.buffer_size
        DataManager.update_size = Params.update_size
        DataManager.minibatch_size = Params.minibatch_size
        DataManager.use_disk = Params.use_disk
        DataManager.num_nodes = Params.num_nodes
        DataManager.points_per_game = Params.points_per_game
        DataManager.num_nodes = Params.num_nodes
        DataManager.verbose = Params.data_verbose

        # setup model
        BasicGCN.num_nodes = Params.num_nodes
        BasicGCN.map_x = Params.map_x
        BasicGCN.map_y = Params.map_y
        BasicGCN.num_node_features = Params.num_node_features
        BasicGCN.num_edge_features = Params.num_edge_features
        BasicGCN.num_cnn_inputs = Params.num_cnn_inputs
        BasicGCN.model_type = Params.model_type
        BasicGCN.model_dir = active_dir
        BasicGCN.model_name = f'{Params.model_type}_{Params.terrain_type}_{Params.unit_dist}.keras'
        BasicGCN.model_fname = active_dir + BasicGCN.model_name
        BasicGCN.eval_fname = active_dir + Params.eval_file
        BasicGCN.training_fname = active_dir + Params.training_file
        BasicGCN.learning_rate = Params.learning_rate
        BasicGCN.lr_decay = Params.decay_rate
        BasicGCN.metric = Params.metric
        BasicGCN.epsilon = Params.epsilon
        BasicGCN.print_interval = Params.print_interval
        BasicGCN.gcn_layers = Params.gcn_layers
        BasicGCN.lr_type = Params.lr_type
        BasicGCN.dense_layers = Params.dense_layers

        # if active dir does not exist, create it
        if not os.path.exists(active_dir):
            os.makedirs(active_dir)
    
    