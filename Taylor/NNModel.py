# print("Num GPUs Available: ", len(tf.config.list_physical_devices('GPU')))

import tensorflow as tf
from tensorflow import keras
from tensorflow.keras.models import save_model
from tensorflow.keras.callbacks import LearningRateScheduler
from spektral.layers import GCNConv
import numpy as np
import os
import math
from sklearn.metrics import confusion_matrix, classification_report, accuracy_score

class NNModel:

    def __init__(self, model_name):
        self.learning_rate = 0.001
        self.lr_decay = 0.97
        # self.learning_rate = 0.005
        # self.lr_decay = 0.90
        self.lr_type = 'exponential'
        self.metric = 'mae'
        self.loss_type = 'mse'
        
        self.model_type = None
        self.model_name = model_name
        self.model_dir = "./models/"
        self.model_fname = self.model_dir + self.model_name
        # self.training_fname = "34863_19768_28429_data.csv"
        # self.eval_fname = "1783_1186_890_data.csv"

        self.active_model = None

        self.feature_scalers = None
        self.edge_feature_scalers = None
        self.disable_scaling = True
        self.epsilon = 0.5
        self.print_interval = 10

    def interpret_output(self, x):
        if x < -1*self.epsilon:
            return -1
        elif x > self.epsilon:
            return 1
        else:
            return 0
        
    def set_model_name(self, model_name):
        self.model_name = model_name
        self.model_fname = self.model_dir + self.model_name
        print('model name set to ', self.model_name)
    
    def set_model_dir(self, model_dir):
        self.model_dir = model_dir
        self.model_fname = self.model_dir + self.model_name
        print('model dir set to ', self.model_dir)

    def perform_inference(self, inference_input):
        continuous_predictions = self.active_model.predict(inference_input)
        predicted_classes = np.array([self.interpret_output(x) for x in continuous_predictions.flatten()])
        return continuous_predictions, predicted_classes

    # eval set already transformed into correct format to load into model
    def evaluate(self, eval_input, eval_labels, verbose=True):
        print(f"evaluating {len(eval_labels)} points...")
        continuous_predictions = self.active_model.predict(eval_input)
        predicted_classes = np.array([self.interpret_output(x) for x in continuous_predictions.flatten()])

        # Confusion Matrix
        conf_matrix = confusion_matrix(eval_labels, predicted_classes)

        # Classification Report
        class_report = classification_report(eval_labels, predicted_classes, zero_division=0)
        
        if verbose:
            print('first 100 predictions: ', continuous_predictions.flatten()[:100])

            print("Confusion Matrix:")
            print(conf_matrix)

            print("Classification Report:")
            print(class_report)

        accuracy = accuracy_score(eval_labels, predicted_classes)

        return accuracy, conf_matrix, class_report

    def train(self, training_input, training_labels, epochs, batch_size, verbose=True):
        print("training model...")
        
        # lr decay
        def lr_exponential(epoch, lr):
            return lr * self.lr_decay
        cosine_lrs = [self.learning_rate * (math.cos(math.pi * epoch / (epochs/4)) + 1) / 2 for epoch in range(epochs)]
        def cosine_annealing(epoch, lr):
            return cosine_lrs[epoch]
        def lr_constant(epoch, lr):
            return lr
        
        # Create the callback
        if self.lr_type == 'exponential':
            lr_callback = LearningRateScheduler(lr_exponential)
        elif self.lr_type == 'cosine':
            lr_callback = LearningRateScheduler(cosine_annealing)
        else:
            lr_callback = LearningRateScheduler(lr_constant)

        callbacks = [lr_callback]
        
        # Train the model
        if verbose:
            self.active_model.fit(training_input, training_labels, epochs=epochs, batch_size=batch_size, callbacks=callbacks)
        else:
            self.active_model.fit(training_input, training_labels, epochs=epochs, batch_size=batch_size, verbose=0, callbacks=callbacks)

    def load_model(self, model_name=None):
        # If file {model_fname} exists, load the model from the file
        # Otherwise, create a new model
        custom_objects = {
            'GCNConv': GCNConv
        }
        try:
            with keras.utils.custom_object_scope(custom_objects):
                if model_name is not None: 
                    self.update_model_name(model_name)
                    self.active_model = keras.models.load_model(self.model_dir + model_name)
                else:
                    self.active_model = keras.models.load_model(self.model_fname)
                self.compile() # TODO is this right?
            print('loaded saved model ', self.model_fname)
        except Exception as e:
            print(e)
            self.active_model = self.create_model()
            print('created new model')
    
    def set_lr(self, lr):
        old_lr = self.active_model.optimizer.learning_rate.numpy()
        self.learning_rate = lr
        if old_lr != self.learning_rate:
            self.compile()
            print(f"new lr: {self.learning_rate}")

    def compile(self):
        self.active_model.compile(optimizer=keras.optimizers.Adam(learning_rate=self.learning_rate), 
                        loss=self.loss_type, 
                        metrics=[self.metric])
        print('model compiled')


    def save(self, fail_flag=False):
        if fail_flag:
            # add '-fail' before '.keras'
            self.save_as(self.model_fname[:-6] + '-fail.keras')
        else:
            self.save_as(self.model_fname)
    
    def save_as(self, model_fname):
        # if old model exists, delete it
        if os.path.exists(self.model_fname):
            print('overwriting ', self.model_fname)
            os.remove(self.model_fname)
        save_model(self.active_model, model_fname)
        print('saved model ', model_fname)

    def create_model(self):
        pass

    def select_data(self, tensors):
        pass

    def tensorize_data(self, data, labels=None):
        data_tensors = [tf.convert_to_tensor(col, dtype=tf.float32) for col in data]
        if labels is None:
            return data_tensors
        labels = tf.convert_to_tensor(labels, dtype=tf.float32)
        return data_tensors, labels