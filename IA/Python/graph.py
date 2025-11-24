import graphviz

# def dessine_reseau(reseau: neurones.Reseau, view : bool = True) -> None:
#     gr = graphviz.Digraph(reseau.name)

#     for i in range(0, len(reseau.neurones)):
#         neu = reseau.get_neuron(i)
#         gr.node(neu.name, neu.name + ": " + str(neu.value) + " sl " + str(neu.biais))

#     for i in range(0, len(reseau.neurones)):
#         neu = reseau.get_neuron(i)
#         for lien in neu.entrees:
#             if lien.amont:
#                 gr.edge(lien.amont.name, neu.name, str(lien.value()))
#     print(gr)

#     gr.render(view=view, format='png')

def dessine(name: str, nodes: list[tuple[str, str, dict[str, str]]], edges: list[tuple[str, str, str]], do_display : bool = True):
    gr = graphviz.Digraph(name)
    for n in nodes:
        (nname, legend, extra) = n
        gr.node(nname, label=f"{nname} : {legend}", **extra)
    for e in edges:
        (amont, aval, legende) = e
        if amont and aval:
            gr.edge(amont, aval, legende)

    # print(gr)

    gr.render(view=do_display, format='png')
