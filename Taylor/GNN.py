import os
import tensorflow as tf
from tensorflow.keras.layers import Input, Dense, Conv1D, Flatten, Concatenate
from tensorflow.keras.models import Model
from spektral.layers import GCNConv  # Assuming Spektral for GCN



def setup_model():
    # Define input layers
    input_tensor_cnn = Input(shape=(64,))
    input_tensor_gcn = Input(shape=(64,))  # Assuming features per node

    # CNN branch
    x_cnn = Conv1D(filters=32, kernel_size=3, activation='relu')(tf.expand_dims(input_tensor_cnn, axis=-1))
    x_cnn = Flatten()(x_cnn)

    # GCN branch (Pseudo-code, adjust based on actual graph structure and library)
    # For a GCN, you typically need an adjacency matrix along with the feature matrix
    # Let's assume adjacency_matrix is available and appropriately processed
    adjacency_matrix = ...  # Define or load your adjacency matrix
    x_gcn = GCNConv(32, activation='relu')([input_tensor_gcn, adjacency_matrix])

    # Merge CNN and GCN outputs
    merged = Concatenate()([x_cnn, x_gcn])

    # Dense layer after merging
    x = Dense(64, activation='relu')(merged)

    # Separate output layers
    output_1 = Dense(1, activation='sigmoid')(x)
    output_2 = Dense(1, activation='sigmoid')(x)

    # Build the model
    model = Model(inputs=[input_tensor_cnn, input_tensor_gcn], outputs=[output_1, output_2])

    model.compile(optimizer='adam', loss='mean_squared_error')


def train_model(model, input_tensors, output_tensors):
    # Assuming input_tensors and output_tensors are available
    model.fit(input_tensors, output_tensors, epochs=10)

def setup_basic_model():



# files_and_directories = os.listdir('.')
# print(files_and_directories)
generate_input_tensors('Taylor/generated_maps/genmap1.tbsmap')