#from typing import Generator, Iterator, Any, Iterable
import sys
import math

from typing import Generic, TypeVar, Iterator, Callable
from collections import deque


T = TypeVar('T')

import graph
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
    name: str
    biais: float = 0.0
    entrees: list['Connexion']
    sorties: list['Connexion']
    value: float = 0.0

    def __init__(self, name : str, seuil: float = 0) -> None:
        self.name = name
        self.entrees = []
        self.sorties = []
        self.biais = seuil
        self.value = 0.0            
    
    def __str__(self) -> str:
        return "n["+self.name+"]"

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


class Connexion:
    """
    Une connexion entre deux neurone. 
    Si il s'agit d'un neurone d'entree (rétine), sa valeur est codée en dur,
    sinon on va chercher la valeur dans le neurone parent, qui
    transmet la meme valeur a tous ses enfants, en modulant en fonction de son poids
    """
    _value: float | None  # cette valeur est calculée par le neurone amont, ou bien mise en dur quand il s'agit d'un neurone d'entree
    amont: Neurone | None  # les neurone de premiere couche n'ont pas d'entree, ils ont juste une valeur intrinseque
    aval: Neurone | None  # le neurone cible
    poids: float  # le biais donné a cette liaison, soit inhibée, soit stimulée

    def __init__(
            self, value: float | None, 
            amont : Neurone | None, 
            aval : Neurone | None, 
            poids: float = 1.0):
        self._value = value
        self.amont = amont
        self.aval = aval
        self.poids = poids

        if self.amont:
            self.amont.sorties += [self]
        if self.aval:
            self.aval.entrees += [self]

        "On est soit un neurone d'entree donc on a pas de parent "
        "et sa valeur est intinseque, soit on a un parent, "
        "mais pas les deux a la fois"
        assert (self._value is not None) ^ (self.amont is not None)

    def __str__(self) -> str:
        return "cx["+str(self.amont) + " <-> " + str(self.aval)+"]"
        
    def value(self) -> float:
        # si on est un neurone d'entree, on retourne sa veleur interne
        if self._value:
            return self._value * self.poids

        if self.amont:
            return self.amont.value
        return 0
    
    def zero(self):
        self._value = 0
    
    def feed(self, value: float) -> None:
        self._value = value


class Reseau:
    """
    Classe qui organise un reseau de neurones
    """
    name: str = "Reseau de neurones"
    neurones : list[Neurone]
    connexions : list[Connexion]
    optiques : list[Neurone]
    sorties : list[Neurone]

    value_f : Callable[[float, float], float]
    "la fonction a utiliser pour le neurone final"
    learning_iterations : int
    "Le nombre d'iterations faites lors de l'apprentissage"

    def __init__(self, value_f : Callable[[float, float], float]) -> None:
        self.neurones = []
        self.optiques = []
        self.sorties = []
        self.connexions = []

        self.value_f = value_f
        self.learning_iterations = 0
    
    def __str__(self) -> str:
        return "Neurones: " + str(len(self.neurones))+" Sorties: " \
        + ", ".join(str(s) for s in self.sorties)

    def compute_entries(self) -> None:
        for n in self.neurones:
            if not n.is_entry():  # on suppose qu'on met les entrees d'abord
                continue
            self.optiques += [n]
    
    def compute_sorties(self) -> None:
        for n in self.neurones:
            if not n.is_sortie():
                continue
            self.sorties += [n]

    def zero_entries(self) -> None:
        for n in self.optiques:
            for e in n.entrees:
                e.zero()

    def feed_entries(self, vals: tuple[float, ...]) -> None:
        for i, v in enumerate(vals):
            self.optiques[i].entrees[0].feed(v)

    def ajout_neurone(self, neurone: Neurone):
        self.neurones += [neurone]
    
    def ajout_relation(self, neurone: Neurone, amont: Neurone | None, value: float | None = None):
        self.connexions.append(Connexion(value, amont=amont, aval=neurone))

    def get_neuron(self, at: int):
        return self.neurones[at]
    
    def classification(self):
        """
        Sort le premier neurone qui a un poids positif.
        """
        for s in self.sorties:
            if s.value > 0:
                return s
        return None
    
    def get_sortie(self, name: str) -> Neurone:
        """
        Retourne le neurone de sortie qui correspond a la classification attendue
        """
        for s in self.sorties:
            if s.name == name:
                return s
        else:
            raise RuntimeError("Neurone sortie", name, "inconnu")
    
    def fire(self) -> None:
        """
        Fait travailler les neurones de la premiere couche, puis ceux
        des couches suivante, etc.
        """
        next_neurons = SetDeque[Neurone]()
        for en in self.optiques:
            next_neurons.append(en)

        while next_neurons:
            n = next_neurons.pop()
            t = 0.0

            for e in n.entrees:
                t += e.value() * e.poids  # somme toutes les entrees
            n.value = self.value_f(t, n.biais)

            for s in n.sorties:
                if s.aval is not None:
                    next_neurons.append(s.aval) # on met la couche suivante

    def fix(self, known: dict[tuple[float, ...], int], outputs: list[str], learning_rate: float):
        """Fixe les poids, en partant de la sortie.
        On essaye chaque couple entree/sortie, et on modifie
        les poids selon l'algorithme de rosenblatt.
        
        Essaye de modifier le poids en fonction de la gigue
        Si on n'obtient pas d'amelioration de la fonction
        d'erreur dans l'un ou l'autre sens, on reduit le pas de
        la gigue. Si le signe de l'erreur change, on reduit le pas,
        sinon on l'augmente.
        
        todo: minima locaux (annealed reheat?)
        todo: penrose
        """
        for idx, i in enumerate(known.items()):
            (vector_i, ou_idx) = i
            for out_n in self.sorties:
                if out_n.name == outputs[ou_idx]:
                    ot = 1
                else:
                    ot = -1
                self.draw(do_display=True, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}")

                self.feed_entries(vector_i)
                self.fire()
                for w in out_n.entrees:
                    print("poids changé", out_n, "entree", w, "avant", w.poids, end=" ", file=sys.stderr)
                    w.poids = w.poids + learning_rate * ot * w.value()
                    print("apres", w.poids, "biais", out_n.biais, end=" ", file=sys.stderr)
                    out_n.biais = out_n.biais + learning_rate * ot
                    print("apres", out_n.biais, file=sys.stderr)

    def train(self, known: dict[tuple[float, ...], int], outputs: list[str], 
              max_iterations: int = 5, learning_rate: float=0.1) -> bool:
        """
        Donne une serie d'entrees et compare la sortie avec la sortie
        attendue. Tant que la sortie ne correspond pas a ce qui est attendu,
        ajuste les poids pour obtenir ce qu'on cherche.

        Essaye :max_iterations: fois avant d'abandonner (ce qui arrive dans les
        configurations de donnees non lineairement separables)
        """
        while True:
            for idx, i in enumerate(known):
                self.learning_iterations += 1
                ot = outputs[idx]
                self.feed_entries(i)
                self.fire()
                c = self.classification()
                if not c:
                    continue
                if c.name != ot:
                    print("resultat insatisfaisant, fix", file=sys.stderr)
                    self.fix(known, outputs, learning_rate)
                    # self.draw(do_display=True, name=self.name + " iteration " + str(max_iterations))
                    break

            else:
                return True
            if self.learning_iterations > max_iterations:
                return False
            
    def draw(self, do_display: bool = True, name: str = ""):
        graph.dessine(
            name or self.name, 
            nodes=[(n.name, str(n.value)) for n in self.neurones], 
            edges=[(c.amont and c.amont.name or "", c.aval and c.aval.name or "", str(c.value())) 
                   for c in self.connexions], 
            do_display=do_display
        )

def mcculloch_pitts_neuron(entries: int, seuil:float = 0) -> Reseau:
    """1943 :
    verifie simplement que la somme des poids est
    superieure a un seuil, le seuil est
    fixe manuellement."""
    """tous les dendrites ont un poids de 1,
    on verifie juste que somme inputs > seuil.
    Il s'agissait d'un montage electronique,
    ou on devait regler le seuil manuellement."""
    def value_f(value:float, seuil:float) -> float:
        r = 0
        if value >= seuil:
            r = 1
        return r

    reseau = Reseau(value_f)

    # entrees
    for i in range(entries):
        neuron = Neurone(str(i+1), seuil=0.1)
        reseau.ajout_neurone(neuron)
        reseau.ajout_relation(neurone=neuron, amont=None, value=0)
    
    # neurone de sortie
    sortie = Neurone("OUT", seuil=seuil)
    reseau.ajout_neurone(sortie)
    reseau.connexions.append(Connexion(None, amont=sortie, aval=None))

    # liaisons
    for i in range(entries):
        reseau.ajout_relation(sortie, reseau.get_neuron(i))

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau


def rosenblatt_perceptron(entries: int, sorties: list[str]) -> Reseau:
    """1957 :
    Chaque dendrite peut avoir un "poids" et les
    entrees sont lineaires plutot que binaires,
    on calcule plutot une probablité qu'une reponse définitive."""

    def heave(v:float, s:float) -> float:
        if v >= s:
            return 1
        else:
            return -1

    def logi(v:float, s:float) -> float:

        return 1 / (1 + math.exp(-v + s))

    reseau = Reseau(logi)

    # entrees
    en: list[Neurone] = []
    for i in range(entries):
        neuron = Neurone("entree " + str(i+1))
        en.append(neuron)
        reseau.ajout_neurone(neuron)
        reseau.ajout_relation(neurone=neuron, amont=None, value=0)
    
    # neurone de sortie
    for sv in sorties:
        
        sortie = Neurone(sv)
        reseau.ajout_neurone(sortie)
        reseau.connexions.append(Connexion(None, amont=sortie, aval=None))

        for ne in en:
            reseau.ajout_relation(sortie, ne)

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau
