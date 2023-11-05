all: modules

modules:
	# Perl modules from cpanfile.snapshot
	cd server && cpanm --installdeps .
	cd server && carton install
	# NPM modules from the package-lock.json file
	cd server && npm install

deploy: modules
	git push --tags	# also push to github repository, don't deploy only to the prod server
	cd server && scripts/deploy
