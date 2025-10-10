import graphviz
import pydot

import neurones

def dessine_reseau(reseau: neurones.Reseau):
    gr = graphviz.Digraph(reseau.name)

    for i in range(0, len(reseau.neurones)):
        neu = reseau.get_neuron(i)
        gr.node(neu.name, neu.name)

    for i in range(0, len(reseau.neurones)):
        neu = reseau.get_neuron(i)
        for (n, poids) in neu.entrees:
            gr.edge(n.name, neu.name, str(poids))
    print(gr)

    gr.render(None, None, True)
    