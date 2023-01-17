all: modules run

modules:
	# Perl modules from cpanfile.snapshot
	cpanm --install deps .
	carton install
	# NPM modules from the package-lock.json file
	npm ci

run:
	scripts/run_server

deploy: modules
	scripts/deploy
