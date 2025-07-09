from NNModel import NNModel
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import Model
from spektral.layers import GCNConv


# child of NNModel
class CombinedCNNModel(NNModel):
    def __init__(self, model_name):
        super().__init__(model_name)
        self.model_type = "CombinedCNN"

        self.num_cnn_inputs = 5
        self.map_x = 8
        self.map_y = 8

        self.num_nodes = 6
        self.num_node_features = 4
        self.num_edge_features = 3 

        self.conv_filters = [32, 64]
        self.dense_layers = [64, 32] 
        

    def create_model(self):
        # GCN inputs
        adjacency_shape = (self.num_nodes, self.num_nodes)
        feature_shape = (self.num_nodes, self.num_node_features)
        
        adjacency_input = keras.Input(shape=adjacency_shape)
        feature_input = keras.Input(shape=feature_shape)
        edge_feature_inputs = [keras.Input(shape=adjacency_shape) for _ in range(self.num_edge_features)]
        
        # CNN inputs - keeping the exact same structure as CombinedModel
        cnn_inputs = [
            layers.Input(shape=(self.map_x, self.map_y)) for _ in range(self.num_cnn_inputs)
        ]
        
        # Graph Input branch - using CNNGraphModel's structure
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
        
        # Combine both graph branches
        x1 = layers.Flatten()(x1)
        x2 = layers.Flatten()(x2)
        x_graphcnn = layers.Concatenate()([x1, x2])
        
        # Dense layers for graph part
        x_graphcnn = layers.Dense(self.dense_layers[0], activation="relu")(x_graphcnn)
        x_graphcnn = layers.Dense(self.dense_layers[1], activation="relu")(x_graphcnn)
        
        # CNN branch - keeping exactly as in CombinedModel
        x_cnn = tf.stack(cnn_inputs, axis=-1)
        
        x_cnn = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Conv2D(32, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.MaxPooling2D((2, 2))(x_cnn)
        
        x_cnn = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Conv2D(64, (3, 3), padding='same', activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.MaxPooling2D((2, 2))(x_cnn)
        
        x_cnn = layers.Flatten()(x_cnn)
        x_cnn = layers.Dense(128, activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Dropout(0.5)(x_cnn)
        x_cnn = layers.Dense(64, activation='relu')(x_cnn)
        x_cnn = layers.BatchNormalization()(x_cnn)
        x_cnn = layers.Dropout(0.3)(x_cnn)
        
        # Combine both branches
        combined = layers.Concatenate()([x_graphcnn, x_cnn])
        
        # Final dense layers
        x = layers.Dense(256, activation='relu')(combined)
        x = layers.BatchNormalization()(x)
        x = layers.Dropout(0.3)(x)
        outputs = layers.Dense(1, activation='tanh')(x)
        
        # Create model - The key fix: Making sure all inputs are explicitly listed in the same order they're used
        model = Model(
            inputs=[adjacency_input, feature_input] + edge_feature_inputs + cnn_inputs,
            outputs=outputs,
            name="combined_cnn_model"  # Added name to help with debugging
        )
        
        # Compile the model
        optimizer = keras.optimizers.Adam(learning_rate=self.learning_rate, clipnorm=1.0)
        model.compile(optimizer=optimizer, loss="mse", metrics=self.metric)
        
        self.active_model = model
        return model
    
    def select_data(self, tensors):
        return tensors