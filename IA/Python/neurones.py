#from typing import Generator, Iterator, Any, Iterable
import sys
import math

from typing import Generic, TypeVar, Iterator, Callable, Iterable, Literal
from collections import deque


T = TypeVar('T')

from . import graph

class SetDeque(Generic[T]):
    """
    Un deque qui verifie l'unicité des
    éléments qui s'y trouvent.
    """
    def __init__(self, kick:Iterable[T] = ()) -> None:
        self._set: set[T] = set()
        self._deque: deque[T] = deque()
        for i in kick:
            self.append(i)
    
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

def heave(value:float, seuil:float) -> float:
    if value > seuil:
        return 1
    elif value < seuil:
        return 0
    else:
        return 0.5

def sign(value:float, seuil:float) -> float:
    if value > seuil:
        return 1
    elif value < seuil:
        return -1
    else:
        return 0

def logi(v:float, s:float) -> float:
    return 1 / (1 + math.exp(-v + s))

def d_logi_from_logi(a: float) -> float:
    """
    a: doit etre une valeur obtenue via logi() précédemment
    """
    return a * (1 - a)

def tanh(v:float, s:float) -> float:
    return math.tanh(v - s)

def d_tanh_from_tanh(a: float) -> float:
    """
    a: doit etre une valeur obtenue via tanh() précédemment
    """
    return 1 - a * a

def softmax(values: list[float]) -> list[float]:
    m = max(values)
    exps = [math.exp(v - m) for v in values]
    s = sum(exps)
    return [e / s for e in exps]

class Neurone:
    name: str
    biais: float = 0.0
    entrees: list['Connexion']
    sorties: list['Connexion']
    value: float = 0.0
    ecart: float # apres feed forward, l'ecart entre le poids souhaité et réel
    proba: float # uniquement pour les neurones de sortie, pour sortir la classification

    value_f : Callable[[float, float], float]
    d_value_f : Callable[[float], float] # si on utilise backprop

    def __init__(
            self, value_f : Callable[[float, float], float], 
            name : str, seuil: float = 0,
            d_value_f: Callable[[float], float] = lambda x : x) -> None:
        self.name = name
        self.entrees = []
        self.sorties = []
        self.biais = seuil
        self.value = 0.0
        self.ecart = 0.0
        self.value_f = value_f
        self.d_value_f = d_value_f
    
    def __str__(self) -> str:
        return "n["+self.name+"]"
    def __repr__(self) -> str:
        return f"n[{self.name}] {self.proba:.3f}"

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
        # attention a ne pas multiplier par le poids car on veut
        # la valeur sans poids lors de la backprop
        if self.raw_value is not None:
            return self.raw_value

        if self.amont:
            return self.amont.value
        return 0
        
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
    nb_hidden_layers : int

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
        self.nb_hidden_layers = 0

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

    def feed_entries(self, vals: tuple[float, ...]) -> None:
        for i, v in enumerate(vals):
            self.optiques[i].entrees[0].feed(v)
        for n in self.neurones:
            n.value = 0

    def classification(self):
        """
        Sort le premier neurone qui a un poids strictement positif.
        """
        for s in self.sorties:
            if s.value > 0:
                return s
        return None
    
    def classification_softmax(self) -> Neurone:
        logits = [n.value for n in self.sorties]
        probs = softmax(logits)

        for n, p in zip(self.sorties, probs):
            n.proba = p  # debug / affichage éventuel

        return max(self.sorties, key=lambda n: n.proba)

    def classification_werbos(self) -> Neurone | None:
        if not self.sorties:
            return None
        return max(self.sorties, key=lambda n: n.value)

    def fire(self) -> None:
        """
        Fait travailler les neurones de la premiere couche, puis ceux
        des couches suivante, etc.
        """
        next_neurons = SetDeque(self.optiques)

        while next_neurons:
            n = next_neurons.pop()
            t = 0.0

            for e in n.entrees:
                t += e.value() * e.poids  # somme toutes les entrees
            n.value = n.value_f(t, n.biais)

            for s in n.sorties:
                if s.aval is not None:
                    next_neurons.append(s.aval) # on met la couche suivante

    def fix_rosen(self, known: tuple[float, ...], 
            output: str, learning_rate: float, debug: bool=True):
        """Fixe les poids, en partant de la sortie.
        On essaye chaque couple entree/sortie, et on modifie
        les poids selon l'algorithme de rosenblatt.

        Fonctionne mal avec couches cachees car chaque neurone
        intermediaire ignore de combien il contribue a la sortie
        vu qu'on utilise une fonction seuil.

        Passer a une fonction sigmoide permet de deriver la contribution
        d'un neurone et de connaitre sa force via la derivation.        
        todo: minima locaux (annealed reheat?)
        todo: penrose
        """

        self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known}, avant feed", skip=not debug)

        self.feed_entries(known)
        self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known}, apres feed", skip=not debug)

        self.fire()  # feed forward
        self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known}, apres propagation", skip=not debug)
        
        for out_n in self.sorties:
            self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known} sortie {out_n.name}, avant back propagation", skip=not debug)
            
            ## si on itere sur la sortie q'on veut renforcer
            if out_n.name == output:
                ot = 1  # ˆy
            else:
                ot = -1
            for w in out_n.entrees:
                self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known} sortie {out_n.name}, fix dendrite {w} avant", skip=not debug)
                w.poids = w.poids + learning_rate * ot * w.value()
                self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known} sortie {out_n.name}, fix dendrite {w} apres", skip=not debug)
            out_n.biais = out_n.biais + learning_rate * ot * -1 ## -1 car quand on veut renforcer un biais pour une sortie donnee, on veut baisser le seuil au lieu de l'augmenter, et vice versa
            self.draw(do_display=False, name=f"{self.name} iteration {self.learning_iterations} feature {known} sortie {out_n.name}, apres fix son propre biais", skip=not debug)

    def fix_chain_rule(self, known: tuple[float, ...], 
            expected_output: dict[str, Literal[-1, 1]], 
            learning_rate: float, debug: bool=True):
        """
        Implément algorithme de werbos avec fonction continue, usage de dérivée
        et chain rule.
        """
        self.feed_entries(known)
        self.fire()

        # erreur de la couche de sortie (ecart entre 1 et -1)
        for out in self.sorties:
            y = expected_output.get(out.name, -1.0)
            a = out.value

            # dL/da * da/dz
            out.ecart = (a - y) * out.d_value_f(a)
            if debug:
                print(f"delta sortie {out.name} = {out.ecart}")

        # erreurs des couches cachées, héritées des erreurs des couches suivantes
        for n in reversed(self.neurones):
            if n.is_sortie() or n.is_entry():
                continue
            ecart = 0
            for cx in n.sorties:
                if cx.aval is not None:
                    ecart += cx.aval.ecart * cx.poids
            n.ecart = ecart * n.d_value_f(n.value)

            if debug:
                print(f"delta caché {n.name} = {n.ecart}")

        # mis a jour des poids en fonction de l'ecart
        for cx in self.connexions:
            if cx.amont is None or cx.aval is None:
                continue

            grad = cx.amont.value * cx.aval.ecart
            cx.poids -= learning_rate * grad

        # mis a jour des biais (forward uses z = sum - bias, so sign flips)
        for n in self.neurones:
            n.biais += learning_rate * n.ecart
            if debug:
                print(f"biais {n.name} = {n.biais}")

    def fix_chain_rule_softmax(
        self,
        known: tuple[float, ...],
        target_output: str,
        learning_rate: float,
        debug: bool = True
    ):
        # -----------------
        # Forward
        # -----------------
        self.feed_entries(known)
        self.fire()

        # -----------------
        # Softmax sur sorties
        # -----------------
        logits = [n.value for n in self.sorties]
        probs = softmax(logits)

        # one-hot target
        y = [1.0 if n.name == target_output else 0.0 for n in self.sorties]

        # -----------------
        # Delta couche sortie
        # -----------------
        for n, p, yi in zip(self.sorties, probs, y):
            n.ecart = p - yi   # ← delta fondamental softmax + CE
            if debug:
                print(f"delta sortie {n.name} = {n.ecart:.4f}")

        # -----------------
        # Delta couches cachées
        # -----------------
        for n in reversed(self.neurones):
            if n.is_sortie() or n.is_entry():
                continue

            err = 0.0
            for cx in n.sorties:
                if cx.aval is not None:
                    err += cx.poids * cx.aval.ecart

            n.ecart = err * n.d_value_f(n.value)

            if debug:
                print(f"delta caché {n.name} = {n.ecart:.4f}")

        # -----------------
        # Mise à jour poids
        # -----------------
        for cx in self.connexions:
            if cx.amont is None or cx.aval is None:
                continue

            grad = cx.amont.value * cx.aval.ecart
            cx.poids -= learning_rate * grad

        # -----------------
        # Mise à jour biais
        # -----------------
        for n in self.neurones:
            if not n.is_entry():
                # forward uses z = sum - bias, so update bias with opposite sign
                n.biais += learning_rate * n.ecart

    def train(self, known: dict[tuple[float, ...], int], outputs: list[str], 
              max_iterations: int = 15, learning_rate: float=0.25, debug: bool=False,
              use_softmax:bool = False) -> bool:
        """
        Donne une serie d'entrees et compare la sortie avec la sortie
        attendue. Tant que la sortie ne correspond pas a ce qui est attendu,
        ajuste les poids pour obtenir ce qu'on cherche.
        Pour permette une convergence on ne retro propage que les doublets 
        qui ne fonctionnent pas.

        Essaye :max_iterations: fois avant d'abandonner (ce qui arrive dans les
        configurations de donnees non lineairement separables)

        On prend un learnig rate multiple de 2 qui evite d'avoir des mantisses illisibles
        """
        while True:
            self.learning_iterations += 1
            need_fixing = False
            for feat, o_idx in known.items():
                ot = outputs[o_idx]
                self.feed_entries(feat)
                self.fire()
                if use_softmax:
                    c = self.classification_werbos()
                else:
                    c = self.classification()
                if not c or c.name != ot: ## aucune sortie ne s'allume ou la mauvaise sortie s'allume
                    if debug:
                        print(f"resultat insatisfaisant feature {feat}, classification {c} fix", file=sys.stderr)
                    if use_softmax:
                        self.fix_chain_rule(feat, {ot: 1}, learning_rate, debug)
                        # self.fix_chain_rule_softmax(feat, ot, learning_rate, debug)
                    else:
                        self.fix_rosen(feat, ot, learning_rate, debug) # on ne fix que l'exemple qui ne fonctionne pas, on force o_idx a zero
                    # self.draw(do_display=True, name=self.name + " iteration " + str(max_iterations))
                    need_fixing = True
                else:
                    if debug:
                        print(f"resultat correct feature {feat}, classification {c}", file=sys.stderr)
            ## plus besoin de fix
            if need_fixing == False:
                return True
            if self.learning_iterations > max_iterations:
                # self.draw()
                return False

    def draw(self, do_display: bool = True, name: str = "", skip: bool=False):
        if skip: ## evite d'avoir des if debug: partout
            return
        graph.dessine(
            f"{self.graphviz_draws} {name or self.name}", 
            nodes=
              [(n.name, f"{n.value:.3f} ~ {n.biais:.3f}" + (f" d: {n.ecart:.3f}" if n.ecart else ""), {'':''}) for n in self.neurones]
              + [("optique", "optique", {'color': 'green'})]
              + [("sortie", "sortie", {'color': 'red'})], 
            edges=[(
                c.amont and c.amont.name or "optique", 
                c.aval and c.aval.name or "sortie", 
                f"{c.poids:.3f}" +  (f" ({c.raw_value:.3f})" if c.raw_value is not None else "")) 
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
        neuron = Neurone(heave, str(i+1), seuil=0.1)
        reseau.neurones += [neuron]
        reseau.connexions += [Connexion(0, amont=None, aval=neuron)]
    
    # neurone de sortie
    sortie = Neurone(heave, "OUT", seuil=seuil)
    reseau.neurones += [sortie]
    reseau.connexions.append(Connexion(None, amont=sortie, aval=None))

    # liaisons
    for i in range(entries):
        reseau.connexions += [Connexion(None, reseau.neurones[i], sortie)]

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau


def rosenblatt_perceptron(num_entries: int, sorties: list[str], hidden: list[int]= []) -> Reseau:
    """1957 :
    Chaque dendrite peut avoir un "poids" et les
    entrees sont lineaires plutot que binaires,
    on calcule plutot une probablité qu'une reponse définitive.
    
    Plus tard viennent les couches cachées.
    On utilise 
    """

    reseau = Reseau()
    
    # entrees
    en: list[Neurone] = []
    for i in range(num_entries):
        neuron = Neurone(act_identity, "entree " + str(i+1), seuil=0.1)
        en.append(neuron)
        reseau.neurones += [neuron]
        reseau.connexions += [Connexion(value=0, amont=None, aval=neuron)]
    
    # neurone de sortie
    for sv in sorties:
        
        sortie = Neurone(sign, sv)
        reseau.neurones += [sortie]
        reseau.connexions += [Connexion(None, amont=sortie, aval=None)]

        for ne in en:
            reseau.connexions += [Connexion(None, ne, sortie)]

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau

def werbos_hidden(num_entries: int, sorties: list[str], hidden: list[int]= []) -> Reseau:
    """1974 :
    Plutot que d'utiliser une fonction seuil qui occulte
    la contribution de chaque neurone a la sortie finale,
    on utilise une fonction derivable, typiquement sigmoide
    ou tanh comme fonction d'activation, et emploie cette 
    derivee avec la regle de chainage pour modifier 
    les poids de maniere lineaire.
    """

    reseau = Reseau()
    reseau.nb_hidden_layers = len(hidden)
    
    # entrees
    en: list[Neurone] = []
    for i in range(num_entries):
        neuron = Neurone(act_identity, "entree " + str(i+1), seuil=0.1)
        en.append(neuron)
        reseau.neurones += [neuron]
        reseau.connexions += [Connexion(value=0, amont=None, aval=neuron)]

    # couches intermediaires
    nen: list[Neurone] = []
    for num_inter, inter in enumerate(hidden):
        for i in range(inter):
            neuron = Neurone(tanh, f"h{num_inter}-{i}", d_value_f=d_tanh_from_tanh)
            nen.append(neuron)
            reseau.neurones += [neuron]
            for ne in en:
                reseau.connexions += [Connexion(None, ne, neuron)]
        en = nen

    # neurone de sortie
    for sv in sorties:
        
        sortie = Neurone(tanh, sv, d_value_f=d_tanh_from_tanh)
        reseau.neurones += [sortie]
        reseau.connexions += [Connexion(None, amont=sortie, aval=None)]

        for ne in en:
            reseau.connexions += [Connexion(None, ne, sortie)]

    reseau.compute_entries()
    reseau.compute_sorties()

    return reseau
