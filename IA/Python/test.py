import unittest

import neurones

class TestPittsMacCulloch(unittest.TestCase):
    """
    Pitts et Murdoch supportent le OR et le AND
    """
    def test_pitts_or(self) -> None:
        """Test un reseau qui simule le or, ici la somme doit juste etre plus grande ou egale que 1."""

        BATCH = 10

        test_or = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=1)
        
        "On test un neurone qui simule le OR, en mettant"
        "chaque dendrite a 1 successivement"
        for entree in test_or.optiques:
            test_or.zero_entries()
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 0)

            test_or.zero_entries()
            entree.entrees[0].feed(1)
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 1)

    def test_pitts_and(self) -> None:
        """Teste que ce reseau de neurone peut simuler un AND."""
        BATCH = 10

        test_and = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=BATCH)
        test_and.name = "mcculloch and"
        test_and.zero_entries()

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

        res = neurones.mcculloch_pitts_neuron(entries=2, seuil=1)

        for i, inp in enumerate(inputs):
            res.feed_entries(inp)
            res.fire()
            self.assertEqual(res.sorties[0].value, outputs[i])

    def test_pitts_matrix_and(self) -> None:
        inputs = [(0.,0.), (0.,1.), (1.,0.), (1.,1.)]
        outputs = [0, 0, 0, 1]

        res = neurones.mcculloch_pitts_neuron(entries=2, seuil=2)

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

        res = neurones.rosenblatt_perceptron(entries=2, sorties=outputs)
        res.name = "Rosen or"
        res.draw()
        res.train(inputs, outputs)

        for (inp, out) in inputs.items():
            res.feed_entries(inp)
            res.fire()
            self.assertEqual(res.sorties[0].value, out)

    @unittest.skip("")
    def test_rosenblatt_and(self) -> None:
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {(0.,0.): 0, (0.,1.): 0, (1.,0.): 0, (1.,1.): 1}

        test_and = neurones.rosenblatt_perceptron(entries=2, sorties=outputs)
        test_and.name = "Rosen and"
        test_and.train(inputs, outputs)

        for i, inp in enumerate(inputs):
            test_and.feed_entries(inp)
            test_and.fire()
            self.assertEqual(test_and.sorties[0].value, outputs[i])

    @unittest.skip("")
    def test_rosenblatt_not(self) -> None:
        outputs = ["0", "1"]
        inputs: dict[tuple[float, ...], int] = {(0.,): 1, (1.,): 0}

        test_not = neurones.rosenblatt_perceptron(entries=1, sorties=outputs)
        test_not.name = "Rosen not"
        test_not.train(inputs, outputs)

        # graph.dessine_reseau(test_not)

        for i, inp in enumerate(inputs):
            test_not.feed_entries(inp)
            test_not.fire()
            self.assertEqual(test_not.sorties[0].value, outputs[i])

class TestLearnPerceptron(unittest.TestCase):
    def test_learn_and(self):
        pass

class TestDeep(unittest.TestCase):
    """
    Des couches cachées permettent de gérer le XOR
    """
    def test_rosen_xor(self) -> None:
        pass

def main():

    unittest.main()


if __name__ == "__main__":
    main()

