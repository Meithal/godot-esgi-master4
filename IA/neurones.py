#from typing import Generator, Iterator, Any, Iterable
from typing import Iterable, Generic, TypeVar, Iterator
from collections import deque

import unittest

T = TypeVar('T')

class SetDeque(Generic[T]):
    """Un deque qui verifie l'unicité des
    éléments qui s'y trouvent."""
    def __init__(self) -> None:
        self._set: set[T] = set()
        self._deque: deque[T] = deque()
    
    def append(self, value: T):
        if value in self._set:
            return
        if value is None:  # on ignore les None dans le cas ou on traite les neurones de sortie dont le neurone aval est None
            return
        self._set.add(value)
        self._deque.append(value)
    
    def pop(self) -> T:
        value = self._deque.pop()
        self._set.remove(value)
        return value
    
    def __iter__(self) -> Iterator[T]:
        return iter(self._deque)
        
    def __len__(self):
        return len(self._deque)

class Neurone:
    nom: str
    biais: float = 0.0
    entrees: list['Connexion']
    sorties: list['Connexion']

    def __init__(self, name : str, seuil: float = 0):
        self.name = name
        self.entrees = []
        self.sorties = []
        self.biais = seuil

    def is_entry(self) -> bool:
        for e in self.entrees:
            if e.amont is None:
                return True
        return False
    
    def is_sortie(self) -> bool:
        for e in self.sorties:
            if e.aval is None:
                return True
        return False


    def compute_sortie(self):
        pass

class Connexion:
    """
    Une connexion entre deux neurone. Les neurones ont une valeur codee en dur
    """
    value: float  # cette valeur est calculée par le neurone amont, ou bien mise en dur quand il s'agit d'un neurone d'entree
    amont: Neurone | None  # les neurone de premiere couche n'ont pas d'entree, ils ont juste une valeur intrinseque
    aval: Neurone  # le neurone cible
    poids: float  # le biais donné a cette liaison, soit inhibée, soit stimulée

    def __init__(self, value, amont, aval, poids = 1.0):
        self.value = value
        self.amont = amont
        self.aval = aval
        self.poids = poids

        self.amont.sorties += [self]
        self.aval.entrees += [self]

class Reseau:
    """
    Classe qui organise un reseau de neurones
    """
    name: str = "Reseau de neurones"
    neurones : list[Neurone]
    entrees : list[Neurone]
    sorties : list[Neurone]

    def __init__(self) -> None:
        self.neurones = []
        self.entrees = []
        self.sorties = []

    def compute_entries(self) -> None:
        for n in self.neurones:
            if not n.is_entry():  # on suppose qu'on met les entrees d'abord
                continue
            self.entrees += [n]
    
    def compute_sorties(self) -> None:
        for n in self.neurones:
            if not n.is_sortie():
                continue
            self.sorties += [n]

    def zero_entries(self) -> None:
        for n in self.entrees:
            for e in n.entrees:
                e.value = 0

    def ajout_neurone(self, neurone: Neurone):
        self.neurones += [neurone]
    
    def ajout_relation(self, neurone: Neurone, amont: Neurone):
        pass
        # neurone.ajout_liaison(amont)

    def get_neuron(self, at: int):
        return self.neurones[at]
    
    def fire(self) -> None:
        """
        Fait travailler les neurones de la premiere couche, puis ceux
        des couches suivante, etc.
        """
        next_neurons = SetDeque[Neurone]()
        for en in self.entrees:
            next_neurons.append(en)
        while next_neurons:
            n = next_neurons.pop()
            t = 0.0

            for e in n.entrees:
                t += e.value * e.poids  # somme toutes les entrees
            for s in n.sorties:
                s.value = t  # met la valeur qu'on vient de calculer sur ses sorties
                next_neurons.append(s.aval) # on met la couche suivante
            

def mcculloch_pitts_neuron(entries: int, seuil = 0) -> Reseau:
    """1946 :
    verifie simplement que la somme des poids est
    superieure a un seuil, le seuil est
    fixe manuellement."""
    """tous les dendrites ont un poids de 1 (booleen),
    on verifie juste que somme inputs > seuil.
    En 1946 il n'y avait pas d'ordinateur grand
    public donc il s'agissait d'un montage electronique,
    comme le perceptron de rosenblatt. Ce n'est donc
    pas un systeme capable de se modifier lui meme."""
    reseau = Reseau()
    for i in range(entries):
        neuron = Neurone(str(i+1), seuil=0)
        reseau.ajout_neurone(neuron)
    
    # neurone de sortie
    sortie = Neurone("OUT", seuil=seuil)
    reseau.ajout_neurone(sortie)
    for i in range(entries):
        reseau.ajout_relation(sortie, reseau.get_neuron(i))

    return reseau


def rosenblatt_perceptron(entries: int) -> Reseau:
    """1957 :
    Chaque dendrite peut avoir un "poids" et les
    entrees sont lineaires plutot que binaires,
    on calcule plutot une probablité qu'une reponse définitive."""
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

class TestPittsMacCulloch(unittest.TestCase):
    def test_pitts(self) -> None:
        """Test un reseau qui simule le or, ici la somme doit
        juste etre plus grande ou egale que 1."""

        BATCH = 10

        test_or = mcculloch_pitts_neuron(entries=BATCH, seuil=1)
        for entree in test_or.entrees:
            test_or.zero_entries()
            #entree.
            entree.entrees[0].value = 1

            test_or.fire()

            self.assertEqual(test_or.sorties[0], 1)


def main():

    unittest.main()


if __name__ == "__main__":
    main()

