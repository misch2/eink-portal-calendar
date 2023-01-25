all: modules run

modules:
	# Perl modules from cpanfile.snapshot
	cd server && cpanm --installdeps .
	cd server && carton install
	# NPM modules from the package-lock.json file
	cd server && npm ci

run:
	cd server && scripts/run_server

deploy: modules
	cd server && scripts/deploy

deploy_test_image_only:
	cd server && scripts/deploy_test_image_only

