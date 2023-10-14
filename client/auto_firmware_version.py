#!/usr/bin/env python

# https://community.platformio.org/t/how-to-build-got-revision-into-binary-for-version-output/15380/5

import subprocess
import os
import sys
from datetime import datetime

def get_firmware_specifier_build_flag():
    # ret = subprocess.run(["git", "describe"], stdout=subprocess.PIPE, text=True) #Uses only annotated tags
    #ret = subprocess.run(["git", "describe", "--tags"], stdout=subprocess.PIPE, text=True) #Uses any tags
    ret = subprocess.run(["git", "rev-parse", "--short", "HEAD"], stdout=subprocess.PIPE, text=True) #Uses only annotated tags
    git_revision = ret.stdout.strip()  # git revision

    date = datetime.today().strftime('%Y%m%d.%H%M%S')

    build_version = date + ".rev_" + git_revision

    build_flag = "-DAUTO_VERSION=\\\"" + build_version + "\\\""
    print ("Firmware Revision: " + build_version)
    return (build_flag)

os.environ["BUILD_FLAGS"] = get_firmware_specifier_build_flag()
