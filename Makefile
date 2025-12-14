.PHONY: test_ia_lib clean

test_ia_lib:
	rm -f *.gv
	rm -f *.gv.png
	./IA/Python/venv/bin/python -m unittest ./IA/Python/test.py

clean:
	rm *.gv
	rm *.gv.png
