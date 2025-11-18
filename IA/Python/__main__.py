import graph
import neurones
#from graphviz import Source

res = neurones.rosenblatt_perceptron(4)

graph.dessine_reseau(reseau=res)

# s = Source.from_file("Reseau de neurones.gv")
#graphviz.Digraph()

#s.render(None, None, True)
