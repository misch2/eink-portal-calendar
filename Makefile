all: modules run

modules:
	# Perl modules from cpanfile.snapshot
	cpanm --installdeps .
	carton install
	# NPM modules from the package-lock.json file
	npm ci

run:
	server/scripts/run_server

deploy: modules
	server/scripts/deploy
