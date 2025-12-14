import unittest

from . import neurones

class TestDeque(unittest.TestCase):
    """
    Teste deque
    """
    def test_good_order(self) -> None:
        next_neurons = neurones.SetDeque[int]()
        next_neurons.append(1)
        next_neurons.append(2)
        val = next_neurons.pop()
        self.assertEqual(val, 1) ## on verifie bien qu'on enleve la valeur mise en premier
        next_neurons.append(3)
        next_neurons.append(4)
        self.assertEqual(next_neurons.pop(), 2)
        self.assertEqual(len(next_neurons), 2)

class TestPittsMacCulloch(unittest.TestCase):
    """
    Pitts et Murdoch supportent le OR et le AND
    """

    def test_pitts_or(self) -> None:
        """Test un reseau qui simule le or, ici la somme doit juste etre plus grande ou egale que 1."""

        BATCH = 10

        test_or = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=.9)
        
        "On test un neurone qui simule le OR, en mettant"
        "chaque dendrite a 1 successivement"
        for entree in test_or.optiques:
            test_or.feed_entries((0,)*BATCH)
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 0)

            test_or.feed_entries((0,)*BATCH)
            entree.entrees[0].feed(1)
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 1)

    def test_pitts_and(self) -> None:
        """
        Teste que ce reseau de neurone peut simuler un AND.
        """
        BATCH = 10

        test_and = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=BATCH - 0.1)
        test_and.name = "mcculloch and"
        test_and.feed_entries((0,)*BATCH)

        test_and.fire()
        # graph.dessine_reseau(reseau=test_and)

        self.assertEqual(test_and.sorties[0].value, 0)

        test_and.optiques[0].entrees[0].feed(1)

        test_and.fire()

        self.assertEqual(test_and.sorties[0].value, 0)

        for entree in test_and.optiques:
            entree.entrees[0].feed(1)

        test_and.fire()
        # graph.dessine_reseau(test_and, False)
        self.assertEqual(test_and.sorties[0].value, 1)

    def test_pitts_matrix_or(self) -> None:
        inputs = [(0.,0.), (0.,1.), (1.,0.), (1.,1.)]
        outputs = [0., 1., 1., 1.]

        res = neurones.mcculloch_pitts_neuron(entries=2, seuil=.9)

        for i, inp in enumerate(inputs):
            res.feed_entries(inp)
            res.fire()
            self.assertEqual(res.sorties[0].value, outputs[i])

    def test_pitts_matrix_and(self) -> None:
        inputs = [(0.,0.), (0.,1.), (1.,0.), (1.,1.)]
        outputs = [0, 0, 0, 1]

        res = neurones.mcculloch_pitts_neuron(entries=2, seuil=1.9)

        for i, inp in enumerate(inputs):
            res.feed_entries(inp)
            res.fire()
            self.assertEqual(res.sorties[0].value, outputs[i])

class TestRosenblattWeights(unittest.TestCase):
    """
    Les poids negatifs de rosenblatt simulent le NOT
    """
    def test_rosenblatt_or(self) -> None:
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {(0., 0.):0, (0.,1.):1, (1.,0.): 1, (1.,1.): 1}

        res = neurones.rosenblatt_perceptron(num_entries=2, sorties=outputs)
        res.name = "Rosen or"
        # res.draw()
        res.train(inputs, outputs, debug=False)

        for (inp, out) in inputs.items():
            res.feed_entries(inp)
            res.fire()

            self.assertIsNotNone(res.classification())
            self.assertEqual(res.sorties[out].value, 1)

    def test_rosenblatt_and(self) -> None:
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {(0.,0.): 0, (0.,1.): 0, (1.,0.): 0, (1.,1.): 1}

        test_and = neurones.rosenblatt_perceptron(num_entries=2, sorties=outputs)
        test_and.name = "Rosen and"
        test_and.train(inputs, outputs)

        for (inp, out) in inputs.items():
            test_and.feed_entries(inp)
            test_and.fire()
            self.assertIsNotNone(test_and.classification())
            self.assertEqual(test_and.sorties[out].value, 1)

    def test_rosenblatt_not(self) -> None:
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {(0.,): 1, (1.,): 0}

        test_not = neurones.rosenblatt_perceptron(num_entries=1, sorties=outputs)
        test_not.name = "Rosen not"
        test_not.train(inputs, outputs)

        # test_not.draw()

        for (inp, out) in inputs.items():
            test_not.feed_entries(inp)
            test_not.fire()
            self.assertIsNotNone(test_not.classification())
            self.assertEqual(test_not.sorties[out].value, 1)

    def test_rosenblatt_xor_one_layer(self) -> None:
        """
        Pour que celui ci marche, il faut implementer une couche cachée.
        """
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {
            (0.,0.): 0, (0.,1.): 1, (1.,0.): 1, (1.,1.): 0
        }

        test_xor = neurones.rosenblatt_perceptron(
            num_entries=len(next(iter(inputs))), sorties=outputs)
        test_xor.name = "Rosen xor"
        success = test_xor.train(inputs, outputs)

        self.assertFalse(success)

        # test_xor.draw()

class TestDeep(unittest.TestCase):
    """
    Des couches cachées permettent de gérer le XOR
    """
    def test_xor(self) -> None:
        """
        Implementation couche cachée.
        """
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {
            (0.,0.): 0, (0.,1.): 1, (1.,0.): 1, (1.,1.): 0
        }

        test_xor = neurones.werbos_hidden(
            num_entries=len(next(iter(inputs))), 
            sorties=outputs,
            hidden = [2], # une couche de deux neurones
        )

        # test_xor.draw()
        test_xor.name = "Werbos xor"
        success = test_xor.train(inputs, outputs, use_softmax=True, debug=False, max_iterations=1000)

        self.assertTrue(success)
        print("iters", test_xor.learning_iterations)
        test_xor.draw()

def main():

    unittest.main()


if __name__ == "__main__":
    main()

