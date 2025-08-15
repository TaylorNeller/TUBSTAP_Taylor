from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import save_model
from spektral.layers import GCNConv


# child of NNModel
class GCNBasicModel(NNModel): # Only uses original 3 input matrices
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "GCNBasic"

        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 1 

        # self.gcn_layers = [16, 4]
        # self.dense_layers = [144, 512]
        self.gcn_layers = [16, 32]
        self.dense_layers = [144, 512]

    def create_model(self):
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)

        # Define the GCN model architecture
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]
        
        
        # Concatenate the edge feature matrices
        if self.num_edge_features > 1:
            concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        else:
            concatenated_edge_features = edge_feature_inputs[0]
        # concatenated_edge_features = edge_feature_inputs[0]

        x = GCNConv(self.gcn_layers[0], activation="relu")([feature_input, adjacency_input]) 
        # x = layers.BatchNormalization()(x)
        x = layers.Concatenate(axis=-1)([x, concatenated_edge_features])
        x = GCNConv(self.gcn_layers[1], activation="relu")([x, adjacency_input])
        # x = layers.BatchNormalization()(x)
        # x = layers.GlobalAveragePooling1D()(x)
        x = layers.Flatten()(x)
        x = layers.Dense(self.dense_layers[0], activation="relu")(x)
        x = layers.Dense(self.dense_layers[1], activation="relu")(x)
        outputs = layers.Dense(1, activation="tanh")(x)
        # outputs = layers.Dense(1)(x)

        model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs, outputs=outputs)

        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)

        
        self.active_model = model
        return model
    
    # Takes list of tensor inputs
    def select_data(self, tensors):
        return tensors[:3]