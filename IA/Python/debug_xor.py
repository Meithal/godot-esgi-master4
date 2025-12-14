from IA.Python import neurones

inputs = {(0.,0.): 0, (0.,1.): 1, (1.,0.): 1, (1.,1.): 0}
outputs = ["0", "1"]

net = neurones.werbos_hidden(num_entries=2, sorties=outputs, hidden=[2])
net.name = "Werbos xor debug"
print(net)

success = net.train(inputs, outputs, use_softmax=True, debug=True, max_iterations=1000)
print('success', success)
print('iterations', net.learning_iterations)
for n in net.neurones:
    print(n.name, 'bias', n.biais)
for c in net.connexions:
    if c.amont and c.aval:
        print('conn', c.amont.name, '->', c.aval.name, 'w', c.poids)
