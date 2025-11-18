import graphviz

import neurones

def dessine_reseau(reseau: neurones.Reseau, view : bool = True) -> None:
    gr = graphviz.Digraph(reseau.name)

    for i in range(0, len(reseau.neurones)):
        neu = reseau.get_neuron(i)
        gr.node(neu.name, neu.name + ": " + str(neu.value) + " s " + str(neu.biais))

    for i in range(0, len(reseau.neurones)):
        neu = reseau.get_neuron(i)
        for lien in neu.entrees:
            if lien.amont:
                gr.edge(lien.amont.name, neu.name, str(lien.value()))
    print(gr)

    gr.render(view=view, format='png')

    