import networkx as nx
import matplotlib.pyplot as plt

# Load the full graph
G = nx.karate_club_graph()

# Select a subset of nodes
nodes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
subgraph = G.subgraph(nodes)

# Visualize the subgraph
nx.draw(G, with_labels=True)
plt.show()
plt.savefig('karate_subgraph.png')
