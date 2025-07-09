from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import save_model

class CNNGraphModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "CNNGraph"

        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 3

        self.conv_filters = [32, 64]
        self.dense_layers = [64, 32] 
        
    def create_model(self):
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)

        # Define inputs (same as GCN)
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]
        
        # Concatenate edge features
        if self.num_edge_features > 1:
            concatenated_edge_features = layers.Concatenate(axis=-1)(edge_feature_inputs)
        else:
            concatenated_edge_features = edge_feature_inputs[0]

        # Reshape inputs to add channel dimension
        x_features = layers.Reshape((self.num_nodes, self.num_node_features, 1))(feature_input)
        x_adj = layers.Reshape((self.num_nodes, self.num_nodes, 1))(adjacency_input)
        x_edge = layers.Reshape((self.num_nodes, self.num_nodes, self.num_edge_features))(concatenated_edge_features)
        
        # Combine adjacency and edge features
        x_graph = layers.Concatenate(axis=-1)([x_adj, x_edge])
        
        # Process node features branch
        x1 = layers.Conv2D(self.conv_filters[0], (3, 3), padding='same', activation='relu')(x_features)
        x1 = layers.MaxPooling2D((2, 2), padding='same')(x1)
        x1 = layers.Conv2D(self.conv_filters[1], (3, 3), padding='same', activation='relu')(x1)
        x1 = layers.MaxPooling2D((2, 2), padding='same')(x1)
        
        # Process graph structure branch
        x2 = layers.Conv2D(self.conv_filters[0], (3, 3), padding='same', activation='relu')(x_graph)
        x2 = layers.MaxPooling2D((2, 2), padding='same')(x2)
        x2 = layers.Conv2D(self.conv_filters[1], (3, 3), padding='same', activation='relu')(x2)
        x2 = layers.MaxPooling2D((2, 2), padding='same')(x2)
        
        # Combine both branches
        x1 = layers.Flatten()(x1)
        x2 = layers.Flatten()(x2)
        x = layers.Concatenate()([x1, x2])
        
        # Dense layers (same as GCN)
        x = layers.Dense(self.dense_layers[0], activation="relu")(x)
        x = layers.Dense(self.dense_layers[1], activation="relu")(x)
        outputs = layers.Dense(1, activation="tanh")(x)

        model = keras.Model(inputs=[adjacency_input, feature_input] + edge_feature_inputs, outputs=outputs)

        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)
        
        self.active_model = model
        return model
    
    def select_data(self, tensors):
        return tensors[:5]