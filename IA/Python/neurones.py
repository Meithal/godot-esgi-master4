#from typing import Generator, Iterator, Any, Iterable
from typing import Generic, TypeVar, Iterator
from collections import deque


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
        return self.name + ":" + str(self.value)

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
            poids = 1.0):
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
        
    def value(self) -> float:
        # si on est un neurone d'entree, on retourne sa veleur interne
        if self._value:
            return self._value * self.poids

        if self.amont:
            return self.amont.value
        return 0

class Reseau:
    """
    Classe qui organise un reseau de neurones
    """
    name: str = "Reseau de neurones"
    neurones : list[Neurone]
    connexions : list[Connexion]
    entrees : list[Neurone]
    sorties : list[Neurone]

    def __init__(self) -> None:
        self.neurones = []
        self.entrees = []
        self.sorties = []
        self.connexions = []
    
    def __str__(self) -> str:
        return "Neurones: " + str(len(self.neurones))+" Sorties: " \
        + ", ".join(str(s) for s in self.sorties)


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
                e._value = 0

    def feed_entries(self, vals: list[float]) -> None:
        for i, v in enumerate(vals):
            self.entrees[i].entrees[0]._value = v

    def ajout_neurone(self, neurone: Neurone):
        self.neurones += [neurone]
    
    def ajout_relation(self, neurone: Neurone, amont: Neurone | None, value: float | None = None):
        self.connexions.append(Connexion(value, amont=amont, aval=neurone))

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
                t += e.value() * e.poids  # somme toutes les entrees
            n.value = 0
            if t >= n.biais:
                n.value = 1  # met la valeur qu'on vient de calculer sur soi
            for s in n.sorties:
                if s.aval is not None:
                    next_neurons.append(s.aval) # on met la couche suivante

    def fix(self) -> bool:
        """Fixe les poids, en partant de la fin.
        Essaye de modifier le poids en fonction de la gigue
        Si on n'obtient pas d'amelioration de la fonction
        d'erreur dans l'un ou l'autre sens, on reduit le pas de
        la gigue. Si le signe de l'erreur change, on reduit le pas,
        sinon on l'augmente.
        
        todo: minima locaux (annealed reheat?)
        todo: penrose
        """
        pass

        return True


def mcculloch_pitts_neuron(entries: int, seuil = 0) -> Reseau:
    """1943 :
    verifie simplement que la somme des poids est
    superieure a un seuil, le seuil est
    fixe manuellement."""
    """tous les dendrites ont un poids de 1,
    on verifie juste que somme inputs > seuil.
    Il s'agissait d'un montage electronique,
    ou on devait regler le seuil manuellement."""
    reseau = Reseau()

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
