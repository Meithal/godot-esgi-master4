.PHONY: install

install: assets/mathjax/es5/tex-chtml-full.js

assets/mathjax/es5/tex-chtml-full.js:	/opt/homebrew/bin/npm
	cd assets/mathjax && \
	npm install typescript && \
	npm run --silent compile && \
	npm run --silent make-components
