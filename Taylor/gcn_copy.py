import tensorflow as tf
# print("Num GPUs Available: ", len(tf.config.list_physical_devices('GPU')))

import Preprocess
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import save_model
from tensorflow.keras.regularizers import l2
from keras.layers import Lambda, Reshape
from spektral.layers import GCNConv
import pandas as pd
import numpy as np
import random
from DataManager import DataManager
import os
import pickle
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import confusion_matrix, classification_report, accuracy_score
import time

class BasicGCN:
    learning_rate = 0.001
    metric = 'accuracy'
    
    model_name = 'gcn_plains_inf-tank.keras'
    model_dir = DataManager.active_dir
    model_fname = model_dir + model_name

    active_model = None

    # input shapes
    num_nodes = 12
    num_node_features = 4
    num_edge_features = 3 

    feature_scalers = None
    edge_feature_scalers = None
    epsilon = 0.5

    @staticmethod
    def interpret_output(x):
        if x < -1*BasicGCN.epsilon:
            return -1
        elif x > BasicGCN.epsilon:
            return 1
        else:
            return 0

    @staticmethod
    def create_gcn_model():
        adjacency_shape = (BasicGCN.num_nodes, BasicGCN.num_nodes)
        feature_shape = (BasicGCN.num_nodes, BasicGCN.num_node_features)

        # Define the GCN model architecture
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(BasicGCN.num_edge_features)]
        mask_input = keras.Input(shape=(BasicGCN.num_nodes,), dtype=tf.float32)
        
        # Concatenate the edge feature matrices
        if BasicGCN.num_edge_features > 1:
            concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        else:
            concatenated_edge_features = edge_feature_inputs[0]
        
        # Masking code
        node_mask = tf.expand_dims(mask_input, axis=-1)
        masked_features = feature_input * node_mask
        
        edge_mask = tf.expand_dims(mask_input, axis=-1) * tf.expand_dims(mask_input, axis=1)
        masked_adjacency = adjacency_input * edge_mask

        x = GCNConv(16, activation="relu")([masked_features, masked_adjacency]) 
        x = GCNConv(4, activation="relu")([x, masked_adjacency])
        x = layers.Flatten()(x)
        x = layers.Dense(144, activation="relu")(x)
        x = layers.Dense(512, activation="relu")(x)
        outputs = layers.Dense(1)(x)

        model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs + [mask_input], outputs=outputs)

        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=BasicGCN.learning_rate, clipvalue=.2)
        model.compile(optimizer=optimizer, loss="mse", metrics=BasicGCN.metric)

        #save the model
        save_model(model, BasicGCN.model_fname) 

        return model
    
    
    @staticmethod
    def update_model_name(model_name):
        BasicGCN.model_name = model_name
        BasicGCN.model_fname = BasicGCN.model_dir + BasicGCN.model_name

    @staticmethod
    def save(fail_flag=False):
        if fail_flag:
            # add '-fail' before '.keras'
            save_model(BasicGCN.active_model, BasicGCN.model_fname[:-6] + '-fail.keras')
        else:
            save_model(BasicGCN.active_model, BasicGCN.model_fname)
    
    @staticmethod
    def save_as(model_name):
        save_model(BasicGCN.active_model, BasicGCN.model_dir + model_name)

    @staticmethod
    def convert_to_tensors(data_column, label_column):
        if BasicGCN.feature_scalers is None or BasicGCN.edge_feature_scalers is None:
            raise ValueError("Scalers are not loaded. Please call load_scalers() first.")
        
        # Prepare the data
        adjacency_matrices = []
        feature_matrices = []
        edge_feature_matrices = [[] for _ in range(BasicGCN.num_edge_features)]  # Assuming 3 edge features
        masks = []

        for _, data_point in data_column.iterrows():
            if type(data_point['Adjacency']) == str:
                adjacency_matrix = np.array(eval(data_point['Adjacency']))
                feature_matrix = np.array(eval(data_point['Feature']))
                edge_features = [np.array(eval(data_point[f'EdgeFeature{i+1}'])) for i in range(BasicGCN.num_edge_features)]
            else:
                adjacency_matrix = np.array(data_point['Adjacency'])
                feature_matrix = np.array(data_point['Feature'])
                edge_features = [np.array(data_point[f'EdgeFeature{i+1}']) for i in range(BasicGCN.num_edge_features)]
            # Scale each column of the feature matrix
            for i in range(BasicGCN.num_node_features):
                feature_matrix[:, i] = BasicGCN.feature_scalers[i].transform(feature_matrix[:, i].reshape(-1, 1)).flatten()

            # Scale the edge features
            for i, edge_feature in enumerate(edge_features):
                edge_features[i] = BasicGCN.edge_feature_scalers[i].transform(edge_feature)

            mask = np.array([1.0 if feature_matrix[node_id][0] != 0 else 0.0 for node_id in range(BasicGCN.num_nodes)])

            adjacency_matrices.append(adjacency_matrix)
            feature_matrices.append(feature_matrix)
            for i, edge_feature in enumerate(edge_features):
                edge_feature_matrices[i].append(edge_feature)
            masks.append(mask)

        # Convert data to TensorFlow tensors
        adjacency_matrices = tf.convert_to_tensor(adjacency_matrices, dtype=tf.float32)
        feature_matrices = tf.convert_to_tensor(feature_matrices, dtype=tf.float32)
        edge_feature_tensors = [tf.convert_to_tensor(edge_feature_matrix, dtype=tf.float32)
                                for edge_feature_matrix in edge_feature_matrices]
        masks = tf.convert_to_tensor(masks, dtype=tf.float32)
        
        transformed_labels = label_column
        labels = tf.convert_to_tensor(transformed_labels.tolist(), dtype=tf.float32)

        return [adjacency_matrices, feature_matrices, edge_feature_tensors, masks, labels]



    @staticmethod
    def fit_scalers(n_data_points=1000):
        print(f"Fitting scalers using {n_data_points} data_points")
        DataManager.update_replay_buffer(n_data_points)
        data_column, labels = DataManager.get_sampled_minibatch(n_data_points, False)

        # Initialize a scaler for each column in the feature matrix
        feature_scalers = [StandardScaler() for _ in range(BasicGCN.num_node_features)]
        edge_feature_scalers = [StandardScaler() for _ in range(BasicGCN.num_edge_features)]

        # Collect all features and edge features to fit scalers
        all_features = [[] for _ in range(BasicGCN.num_node_features)]
        all_edge_features = [[] for _ in range(BasicGCN.num_edge_features)]

        for _, data_point in data_column.iterrows():
            if type(data_point['Adjacency']) == str:
                feature_matrix = np.array(eval(data_point['Feature']))
                edge_features = [np.array(eval(data_point[f'EdgeFeature{i+1}'])) for i in range(BasicGCN.num_edge_features)]
            else:
                feature_matrix = np.array(data_point['Feature'])
                edge_features = [np.array(data_point[f'EdgeFeature{i+1}']) for i in range(BasicGCN.num_edge_features)]
            
            for i in range(BasicGCN.num_node_features):
                all_features[i].append(feature_matrix[:, i])
            
            for i in range(BasicGCN.num_edge_features):
                all_edge_features[i].append(edge_features[i])

        # Fit scalers
        for i in range(BasicGCN.num_node_features):
            all_features[i] = np.concatenate(all_features[i])
            feature_scalers[i].fit(all_features[i].reshape(-1, 1))

        for i in range(BasicGCN.num_edge_features):
            all_edge_features[i] = np.vstack(all_edge_features[i])
            edge_feature_scalers[i].fit(all_edge_features[i])

        # Save scalers
        with open(f'{BasicGCN.model_dir}feature_scalers.pkl', 'wb') as f:
            pickle.dump(feature_scalers, f)
        
        for i in range(BasicGCN.num_edge_features):
            with open(f'{BasicGCN.model_dir}edge_feature_scaler_{i}.pkl', 'wb') as f:
                pickle.dump(edge_feature_scalers[i], f)

        print("Scalers fitted and saved.")

    @staticmethod
    def load_scalers():
        if BasicGCN.feature_scalers is None or BasicGCN.edge_feature_scalers is None:
            # if any of the scaler files do not exist, call fit_scalers()
            if not os.path.exists(f'{BasicGCN.model_dir}feature_scalers.pkl') or \
                not all([os.path.exists(f'{BasicGCN.model_dir}edge_feature_scaler_{i}.pkl') for i in range(BasicGCN.num_edge_features)]):
                print("Scalers not found. Fitting new scalers.")
                BasicGCN.fit_scalers()

            with open(f'{BasicGCN.model_dir}feature_scalers.pkl', 'rb') as f:
                BasicGCN.feature_scalers = pickle.load(f)
            
            BasicGCN.edge_feature_scalers = []
            for i in range(BasicGCN.num_edge_features):
                with open(f'{BasicGCN.model_dir}edge_feature_scaler_{i}.pkl', 'rb') as f:
                    BasicGCN.edge_feature_scalers.append(pickle.load(f))
            print("Scalers loaded.")

    @staticmethod
    def train_model(model, data, epochs, batch_size, verbose=True):
        adjacency_matrices = data[0]
        feature_matrices = data[1]
        edge_feature_tensors = data[2]
        masks = data[3]
        labels = data[4]

        # Ensure all inputs are float32
        adjacency_matrices = tf.cast(adjacency_matrices, dtype=tf.float32)
        feature_matrices = tf.cast(feature_matrices, dtype=tf.float32)
        edge_feature_tensors = [tf.cast(ef, dtype=tf.float32) for ef in edge_feature_tensors]
        masks = tf.cast(masks, dtype=tf.float32)
        labels = tf.cast(labels, dtype=tf.float32)

        # Create a tf.data.Dataset
        dataset = tf.data.Dataset.from_tensor_slices((
            (adjacency_matrices, feature_matrices, *edge_feature_tensors, masks),
            labels
        )).batch(batch_size)

        # Get the optimizer from the model
        optimizer = model.optimizer
        loss_fn = tf.keras.losses.MeanSquaredError()

        @tf.function
        def train_step(inputs, targets):
            with tf.GradientTape() as tape:
                predictions = model(inputs, training=True)
                loss = loss_fn(targets, predictions)
            gradients = tape.gradient(loss, model.trainable_variables)
            optimizer.apply_gradients(zip(gradients, model.trainable_variables))
            return loss, gradients

        # Training loop
        for epoch in range(epochs):
            total_loss = 0
            num_batches = 0
            for batch_inputs, batch_labels in dataset:
                loss, grads = train_step(batch_inputs, batch_labels)
                total_loss += loss
                num_batches += 1

                # Log gradients (you can modify this part to log however you prefer)
                if verbose:
                    for var, grad in zip(model.trainable_variables, grads):
                        tf.summary.histogram(f"{var.name}/gradient", grad, step=epoch * len(dataset) + num_batches)

            avg_loss = total_loss / num_batches
            if verbose:
                print(f"Loss: {avg_loss:.4f}")

                # Print some gradient statistics for a few layers
                print("Gradient statistics:")
                for var, grad in zip(model.trainable_variables[-6:], grads[-6:]):  # Last 6 layers
                    print(f"  {var.name}:")
                    print(f"    Mean: {tf.reduce_mean(grad):.6f}")
                    print(f"    Std Dev: {tf.math.reduce_std(grad):.6f}")
                    print(f"    Min: {tf.reduce_min(grad):.6f}")
                    print(f"    Max: {tf.reduce_max(grad):.6f}")

    @staticmethod
    def training_loop(n_batches, batch_size=None, print_interval=10, n_epochs=1):
        if batch_size is not None:
            DataManager.minibatch_size = batch_size
        else:
            batch_size = DataManager.minibatch_size
        BasicGCN.load_scalers()

        # fill replay buffer so that samples are taken from many games 
        # and not just the few played when sampling minibatch is called
        DataManager.update_replay_buffer(1000)

        # gpu check
        physical_devices = tf.config.list_physical_devices('GPU')
        print("Num GPUs Available: ", len(physical_devices))
        for device in physical_devices:
            print(device)

        
        for e in range(n_epochs):
            print(f"=========================Epoch {e+1}/{n_epochs}=========================")
            if n_epochs > 1:
                if e == 0:
                    DataManager.saving_sampled = True
                    DataManager.saved_replay_buffer = None
                else:
                    DataManager.saving_sampled = False
                    DataManager.new_epoch()
            for i in range(n_batches):
                if i % print_interval == 0:
                    print(f"Batch {i+1}/{n_batches}")
                # start timer
                # DataManager.update_replay_buffer()
                data, labels = DataManager.get_sampled_minibatch(batch_size)
                input_data = BasicGCN.convert_to_tensors(data, labels)
                
                # print every print_interval batches
                if i % print_interval == 0:
                    BasicGCN.train_model(BasicGCN.active_model, input_data, 1, batch_size, True)
                else:
                    BasicGCN.train_model(BasicGCN.active_model, input_data, 1, batch_size, False)

    @staticmethod
    def load_model(model_name=None):
        # If file {model_fname} exists, load the model from the file
        # Otherwise, create a new model
        custom_objects = {
            'GCNConv': GCNConv
        }
        try:
            with keras.utils.custom_object_scope(custom_objects):
                if model_name is not None: 
                    BasicGCN.update_model_name(model_name)
                    BasicGCN.active_model = keras.models.load_model(BasicGCN.model_dir + model_name)
                else:
                    BasicGCN.active_model = keras.models.load_model(BasicGCN.model_fname)
                old_lr = BasicGCN.active_model.optimizer.learning_rate.numpy()
                if old_lr != BasicGCN.learning_rate:
                    keras.backend.set_value(BasicGCN.active_model.optimizer.learning_rate, BasicGCN.learning_rate)
                    BasicGCN.active_model.compile(optimizer=BasicGCN.active_model.optimizer, 
                              loss="mse", 
                              metrics=[BasicGCN.metric])
            print('loaded saved model ', BasicGCN.model_fname)
        except Exception as e:
            print(e)
            BasicGCN.active_model = BasicGCN.create_gcn_model()
            print('created new model')
    
    @staticmethod
    def create_new_model(model_name=None):
        if model_name is not None:
            BasicGCN.update_model_name(model_name)
        # if old model exists, delete it
        if os.path.exists(BasicGCN.model_fname):
            os.remove(BasicGCN.model_fname)
        BasicGCN.active_model = BasicGCN.create_gcn_model()
        print('created new model ', BasicGCN.model_fname)
