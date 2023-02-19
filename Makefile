all: modules

modules:
	# Perl modules from cpanfile.snapshot
	cd server && cpanm --installdeps .
	cd server && carton install
	# NPM modules from the package-lock.json file
	cd server && npm install

deploy: modules
	cd server && scripts/deploy
