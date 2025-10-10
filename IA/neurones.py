class Neurone:
    nom: str
    biais: int = 0.0
    entrees: list[list['Neurone', float]] = [] # une liste de couplets neurones / poids

    def __init__(self, name):
        self.name = name
        self.entrees = []

    def ajout_liaison(self, amont: 'Neurone', poids = 0.0):
        self.entrees += [[amont, poids]]

    def sortie(self):
        pass

class Reseau:
    name: str = "Reseau de neurones"
    neurones : list[Neurone] = []
    sorties : list[Neurone] = []

    def __init__(self):
        self.neurones = []
        self.sorties = []

    def ajout_neurone(self, neurone: Neurone):
        self.neurones += [neurone]
    
    def ajout_relation(self, neurone: Neurone, amont: Neurone):
        neurone.ajout_liaison(amont)

    def get_neuron(self, at: int):
        return self.neurones[at]

def mcculloch_pitts_neuron(entries: int):
    """1946"""
    """verifie simplement que la somme des poids est
    superieure a un seuil, le seuil est
    fixe manuellement"""
    """tous les dendrites ont un poids de 1 (booleen)"""
    """on verifie juste que somme inputs > seuil"""
    pass

def rosenblatt_perceptron(entries: int) -> Reseau:
    """1957"""
    """Le perceptron original n'avait qu'une sortie"""
    """Il a cree le concept de fonction d'activation,
    initialment une porte d'heaveside"""
    """Avec un algorithme d'amelioration"""
    reseau = Reseau()
    for i in range(entries):
        neuron = Neurone(str(i+1))
        reseau.ajout_neurone(neuron)
    
    # neurone de sortie
    sortie = Neurone("OUT")
    reseau.ajout_neurone(sortie)
    for i in range(entries):
        reseau.ajout_relation(sortie, reseau.get_neuron(i))

    return reseau

def dl_or():
    def _chech_error(inputs, reseau):
        pass

    inputs = [[0, 0], [0, 1], [1, 0], [1, 1]]
    results = [0, 1, 1, 1]

    pente = 1.0
    biais = 0.0


    res = rosenblatt_perceptron(4)




def main():

    dl_or()


if __name__ == "__main__":
    main()

