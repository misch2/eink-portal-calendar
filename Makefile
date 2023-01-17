all: install_modules run

install_modules:
	# install Perl modules from cpanfile.snapshot
	carton install
	# install NPM modules from the package-lock.json file
	npm ci

run:
	scripts/run_server
