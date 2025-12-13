#from typing import Generator, Iterator, Any, Iterable
import sys
import math

from typing import Generic, TypeVar, Iterator, Callable
from collections import deque


T = TypeVar('T')

from . import graph

class SetDeque(Generic[T]):
    """
    Un deque qui verifie l'unicité des
    éléments qui s'y trouvent.
    """
    def __init__(self) -> None:
        self._set: set[T] = set()
        self._deque: deque[T] = deque()
    
    def append(self, thing: T):
        if thing in self._set:
            return
        if thing is None:  # on ignore les None dans le cas ou on traite les neurones de sortie dont le neurone aval est None
            return
        self._set.add(thing)
        self._deque.append(thing)

    def pop(self) -> T:
        thing = self._deque.popleft()
        self._set.remove(thing)
        return thing

    def __iter__(self) -> Iterator[T]:
        return iter(self._deque)

    def __len__(self):
        return len(self._deque)

def act_identity(v: float, _: float) -> float:
    return v

def pitts(value:float, seuil:float) -> float:
    r = 0
    if value >= seuil:
        r = 1
    return r

def sign(value:float, seuil:float) -> float:
    if value > seuil:
        return 1
    elif value < seuil:
        return -1
    else:
        return 0

def logi(v:float, s:float) -> float:

    return 1 / (1 + math.exp(-v + s))

class Neurone:
    name: str
    biais: float = 0.0
    entrees: list['Connexion']
    sorties: list['Connexion']
    value: float = 0.0

    value_f : Callable[[float, float], float]

    def __init__(self, value_f : Callable[[float, float], float], name : str, seuil: float = 0) -> None:
        self.name = name
        self.entrees = []
        self.sorties = []
        self.biais = seuil
        self.value = 0.0
        self.value_f = value_f
    
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
    raw_value: float | None  # cette valeur est calculée par le neurone amont, ou bien mise en dur quand il s'agit d'un neurone d'entree
    amont: Neurone | None  # les neurone de premiere couche n'ont pas d'entree, ils ont juste une valeur intrinseque
    aval: Neurone | None  # le neurone cible
    poids: float  # le biais donné a cette liaison, soit inhibée, soit stimulée

    def __init__(
            self, value: float | None, 
            amont : Neurone | None, 
            aval : Neurone | None, 
            poids: float = 1.0):
        self.raw_value = value
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
        assert (self.raw_value is not None) ^ (self.amont is not None)

    def __str__(self) -> str:
        return "cx["+str(self.amont) + " <-> " + str(self.aval)+"]"
        
    def value(self) -> float:
        # si on est un neurone d'entree, on retourne sa veleur interne
        if self.raw_value:
            return self.raw_value * self.poids

        if self.amont:
            return self.amont.value
        return 0
    
    def zero(self):
        self.raw_value = 0
    
    def feed(self, value: float) -> None:
        self.raw_value = value


class Reseau:
    """
    Classe qui organise un reseau de neurones
    """
    name: str = "Reseau de neurones"
    neurones : list[Neurone]
    connexions : list[Connexion]
    optiques : list[Neurone]
    sorties : list[Neurone]

    "la fonction a utiliser pour le neurone final"
    learning_iterations : int
    "Le nombre d'iterations faites lors de l'apprentissage"
    graphviz_draws : int
    "le nombre de fois qu'on a dessiné le reseau, pour generer des noms de fichiers qui se suivent."

    def __init__(self) -> None:
        self.neurones = []
        self.optiques = []
        self.sorties = []
        self.connexions = []

        self.learning_iterations = 0
        self.graphviz_draws = 0

        self.compute_entries()
        self.compute_sorties()
    
    def __str__(self) -> str:
        return "Neurones: " + str(len(self.neurones))+" Sorties: " \
        + ", ".join(str(s) for s in self.sorties)

    def compute_entries(self) -> None:
        """Stocke parmi les neurones ceux qui sont en entree"""
        for n in self.neurones:
            if not n.is_entry():  # on suppose qu'on met les entrees d'abord
                continue
            self.optiques += [n]
    
    def compute_sorties(self) -> None:
        """Identifie les neurones de sortie (classification / regression)"""
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
            n.value = n.value_f(t, n.biais)

            for s in n.sorties:
                if s.aval is not None:
                    next_neurons.append(s.aval) # on met la couche suivante

    def fix(self, known: dict[tuple[float, ...], int], outputs: list[str], learning_rate: float, debug: bool=True):
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

        self.zero_entries()

        for idx, i in enumerate(known.items()):
            (vector_i, ou_idx) = i
            self.feed_entries(vector_i)
            for out_n in self.sorties:
                if out_n.name == outputs[ou_idx]:
                    ot = 1
                else:
                    ot = -1
                if debug:
                    self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}, avant feed")
 
                if debug:
                    self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}, apres feed")
                self.fire()
                if debug:
                    self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}, apres propagation")
                
                for w in out_n.entrees:
                    w.poids = w.poids + learning_rate * ot * w.value()
                    if debug:
                        self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}, fix dendrite {w} apres")
                    out_n.biais = out_n.biais + learning_rate * ot * -1 
                    ## -1 car on utilise le biais intrinsequement (c'est miuex quand il diminue)

                    if debug:
                        self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {idx} {i} sortie {out_n.name}, fix dendrite {w} apres biais")

    def train(self, known: dict[tuple[float, ...], int], outputs: list[str], 
              max_iterations: int = 3, learning_rate: float=0.1) -> bool:
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
                    print("classification introuvable", self.learning_iterations, i, file=sys.stderr)
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
            f"{self.graphviz_draws} {name or self.name}", 
            nodes=
              [(n.name, f"{n.value} ~ {n.biais}", {'':''}) for n in self.neurones]
              + [("optique", "optique", {'color': 'green'})]
              + [("sortie", "sortie", {'color': 'red'})], 
            edges=[(
                c.amont and c.amont.name or "optique", 
                c.aval and c.aval.name or "sortie", 
                f"{c.poids} ({c.raw_value})") 
                   for c in self.connexions], 
            do_display=do_display
        )

        self.graphviz_draws += 1

def mcculloch_pitts_neuron(entries: int, seuil:float = 0) -> Reseau:
    """1943 :
    verifie somme inputs > seuil.
    Pas de notion de poids,
    on devait regler le seuil manuellement, pas d'apprentissage."""

    reseau = Reseau()

    # entrees
    for i in range(entries):
        neuron = Neurone(pitts, str(i+1), seuil=0.1)
        reseau.ajout_neurone(neuron)
        reseau.ajout_relation(neurone=neuron, amont=None, value=0)
    
    # neurone de sortie
    sortie = Neurone(pitts, "OUT", seuil=seuil)
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

    reseau = Reseau()
    
    Reseau()  # inutilise, juste pour pylance

    # entrees
    en: list[Neurone] = []
    for i in range(entries):
        neuron = Neurone(act_identity, "entree " + str(i+1), seuil=0.1)
        en.append(neuron)
        reseau.ajout_neurone(neuron)
        reseau.ajout_relation(neurone=neuron, amont=None, value=0)
    
    # neurone de sortie
    for sv in sorties:
        
        sortie = Neurone(sign, sv)
        reseau.ajout_neurone(sortie)
        reseau.connexions.append(Connexion(None, amont=sortie, aval=None))

        for ne in en:
            reseau.ajout_relation(sortie, ne)

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau
