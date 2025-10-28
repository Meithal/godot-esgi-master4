import unittest

import neurones
import graph


class TestPittsMacCulloch(unittest.TestCase):
    def test_pitts_or(self) -> None:
        """Test un reseau qui simule le or, ici la somme doit juste etre plus grande ou egale que 1."""

        BATCH = 10

        test_or = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=1)
        
        "On test un neurone qui simule le OR, en mettant"
        "chaque dendrite a 1 successivement"
        for entree in test_or.entrees:
            test_or.zero_entries()
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 0)

            test_or.zero_entries()
            entree.entrees[0]._value = 1
            test_or.fire()

            self.assertEqual(test_or.sorties[0].value, 1)

    def test_pitts_and(self) -> None:
        """Teste que ce reseau de neurone peut simuler un AND."""
        BATCH = 10

        test_and = neurones.mcculloch_pitts_neuron(entries=BATCH, seuil=BATCH)
        test_and.zero_entries()

        test_and.fire()
        # graph.dessine_reseau(reseau=test_and)

        self.assertEqual(test_and.sorties[0].value, 0)

        test_and.entrees[0].entrees[0]._value = 1

        test_and.fire()

        self.assertEqual(test_and.sorties[0].value, 0)

        for entree in test_and.entrees:
            entree.entrees[0]._value = 1

        test_and.fire()
        self.assertEqual(test_and.sorties[0].value, 1)

    def test_rosenblatt_or(self) -> None:
        BATCH = 10

        test_and = neurones.rosenblatt_perceptron(entries=BATCH)
        test_and.zero_entries()


def main():

    unittest.main()


if __name__ == "__main__":
    main()

