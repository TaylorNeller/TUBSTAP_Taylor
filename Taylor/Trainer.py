import time
import os

# from NNModel import NNModel
from CNNModel import CNNModel
from GCNModel import GCNModel
from GCN2Model import GCN2Model
from ImprovedGNNModel import ImprovedGNNModel
from CombinedModel import CombinedModel
from CombinedCNNModel import CombinedCNNModel
from CNNGraphModel import CNNGraphModel
from MPGNNModel import MPGNNModel
from DataLoader import DataLoader

class Trainer:

    model_types = {
        "GCN": GCNModel,
        "CNN": CNNModel,
        "Combined": CombinedModel,
        "CNNGraph": CNNGraphModel,
        "CombinedCNN": CombinedCNNModel,
        "MPGNN": MPGNNModel,
        "GCN2": GCN2Model,
        "ImprovedGNN": ImprovedGNNModel
    }

    def __init__(self, model_name, model_type, active_dir=None, keep_better=False, delete_old=False):
        self.keep_better = keep_better
        self.curr_acc = 0
        self.model_name = model_name
        self.delete_old = delete_old
        self.active_dir = active_dir
        self.model_class = Trainer.model_types[model_type]
        self.model = self.model_class(self.model_name)
        self.model.set_model_dir(active_dir)
        if self.delete_old:
            self.model.create_model()
            # remove log file
            log_name = self.model.model_dir + self.model.model_name[:-6] + '.log'
            if os.path.exists(log_name):
                os.remove(log_name)
        else:
            self.model.load_model()
        self.data_loader = DataLoader(self.active_dir)


    def update_training_log(self, str):
        # get log name
        log_name = self.active_dir + self.model_name[:-6] + '.log'
        # if log file does not exist, create it
        if not os.path.exists(log_name):
            with open(log_name, 'w') as f:
                f.write('Training Log\n')
                f.write(str+'\n')
        else:
            with open(log_name, 'a') as f:
                f.write(str+'\n')

    def train(self, training_fname, batch_size, epochs=1):
        self.update_training_log(f"train({training_fname}, {epochs})")
        data, labels = None, None
        if training_fname.endswith('.csv'):
            data, labels = self.data_loader.load_dataframe(training_fname)
            data, labels = self.data_loader.tensorize_data(data, labels)
        else:
            data, labels = self.data_loader.load_tensors(training_fname)
        selected_data = self.model.select_data(data)
        self.model.train(selected_data, labels, batch_size=batch_size, epochs=epochs)
        self.model.save()

    def eval(self, eval_fname):
        self.update_training_log(f"eval({eval_fname})")
        data, labels = None, None
        if eval_fname.endswith('.csv'):
            data, labels = self.data_loader.load_dataframe(eval_fname)
            data, labels = self.data_loader.tensorize_data(data, labels)
        else:
            data, labels = self.data_loader.load_tensors(eval_fname)
        selected_data = self.model.select_data(data)
        accuracy, conf_matrix, class_report = self.model.evaluate(selected_data, labels)

        self.update_training_log(f"accuracy: {accuracy}")
        self.update_training_log(f"{conf_matrix}")