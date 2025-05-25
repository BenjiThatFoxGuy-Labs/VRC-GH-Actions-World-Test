#!/bin/bash

# Unity version to install
UNITY_VERSION="2022.3.22f1"

# Install Rust and Cargo
echo "Installing Rust..."
sudo -i curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
sudo -i source "$HOME/.cargo/env"

# Install cargo-binstall
echo "Installing cargo-binstall..."
sudo -i curl -L --proto '=https' --tlsv1.2 -sSf https://raw.githubusercontent.com/cargo-bins/cargo-binstall/main/install-from-binstall-release.sh | bash

# Add cargo binaries to PATH for the rest of the script
sudo -i export PATH="$HOME/.cargo/bin:$PATH"

# Install vrc-get using cargo-binstall
echo "Installing vrc-get..."
sudo -i cargo binstall -y vrc-get

# Add VPM Repositories
sudo -i vrc-get repo add https://adjerry91.github.io/VRCFaceTracking-Templates/index.json
sudo -i vrc-get repo add https://api.vrlabs.dev/listings/category/Components
sudo -i vrc-get repo add https://api.vrlabs.dev/listings/category/Dependencies
sudo -i vrc-get repo add https://api.vrlabs.dev/listings/category/Essentials
sudo -i vrc-get repo add https://api.vrlabs.dev/listings/category/Systems
sudo -i vrc-get repo add https://codec-xyz.github.io/photo_frame_manager/index.json
sudo -i vrc-get repo add https://drblackrat.github.io/vpm-listing/index.json
sudo -i vrc-get repo add https://enitimeago.github.io/vpm-repos/index.json
sudo -i vrc-get repo add https://esperecyan.github.io/VRMConverterForVRChat/registry.json
sudo -i vrc-get repo add https://furality.github.io/vcc-packages/index.json
sudo -i vrc-get repo add https://github.com/Krysiek/CuteDancer/raw/vpm/CuteDancer.json
sudo -i vrc-get repo add https://hai-vr.github.io/vpm-listing/index.json
sudo -i vrc-get repo add https://kemocade.github.io/Kemocade.Vrc.Data.Extensions/index.json
sudo -i vrc-get repo add https://krysiek.github.io/CuteDancer/index.json
sudo -i vrc-get repo add https://kurone-kito.github.io/vpm/index.json
sudo -i vrc-get repo add https://kurotu.github.io/vpm-repos/vpm.json
sudo -i vrc-get repo add https://labthe3rd.github.io/PZCameraSystem/index.json
sudo -i vrc-get repo add https://lilxyzw.github.io/vpm-repos/vpm.json
sudo -i vrc-get repo add https://magmamcnet.github.io/VPM/index.json
sudo -i vrc-get repo add https://mmmaellon.github.io/MMMaellonVCCListing/index.json
sudo -i vrc-get repo add https://orels1.github.io/orels-Unity-Shaders/index.json
sudo -i vrc-get repo add https://poiyomi.github.io/vpm/index.json
sudo -i vrc-get repo add https://pokeyi.dev/vpm-packages/index.json
sudo -i vrc-get repo add https://repo.buddyworks.wtf/index.json
sudo -i vrc-get repo add https://spokeek.github.io/vpm-repository/index.json
sudo -i vrc-get repo add https://suzuryg.github.io/vpm-repos/vpm.json
sudo -i vrc-get repo add https://vcc.pawlygon.net/index.json
sudo -i vrc-get repo add https://vcc.vrcfury.com/
sudo -i vrc-get repo add https://vpm.anatawa12.com/vpm.json
sudo -i vrc-get repo add https://vpm.benjifox.gay/index.json
sudo -i vrc-get repo add https://vpm.dreadscripts.com/listings/main.json
sudo -i vrc-get repo add https://vpm.nadena.dev/vpm.json
sudo -i vrc-get repo add https://vpm.pimaker.at/index.json
sudo -i vrc-get repo add https://vpm.techanon.dev/index.json
sudo -i vrc-get repo add https://vpm.thry.dev/index.json
sudo -i vrc-get repo add https://wholesomevr.github.io/SPS-Configurator/index.json
sudo -i vrc-get repo add https://i5ucc.github.io/vpm/main.json
sudo -i vrc-get repo add https://pandrabox.github.io/vpm/index.json
sudo -i vrc-get repo add https://kb10uy.github.io/vrc-repository/index.json
sudo -i vrc-get repo add https://tr1turbo.github.io/BlendShare/index.json
sudo -i vrc-get repo add https://waya0125.com/vpm
sudo -i vrc-get repo add https://foxscore.dev/vpm/index.json
sudo -i vrc-get repo add https://tliks.github.io/vpm-repos/index.json
sudo -i vrc-get repo add https://www.matthewherber.com/Happys-VRC-tools/index.json
sudo -i vrc-get repo add https://d4rkc0d3r.github.io/vpm-repos/main.json
sudo -i vrc-get repo add https://vpm.narazaka.net/index.json
sudo -i vrc-get repo add https://xtlcdn.github.io/vpm/index.json
sudo -i vrc-get repo add https://vpm.bluwizard.net/index.json
sudo -i vrc-get repo add https://aurycat.github.io/vpm/index.json
sudo -i vrc-get repo add https://lastationvrchat.github.io/Lastation-Package-Listing/index.json
sudo -i vrc-get repo add https://vpm.ureishi.net/repos.json
sudo -i vrc-get repo add https://vpm.vrsl.dev/index.json
sudo -i vrc-get repo add https://nidonocu.github.io/virtual-gryphon-packages/index.json
sudo -i vrc-get repo add https://z3y.github.io/vpm-package-listing/index.json
sudo -i vrc-get repo add https://page.853lab.com/vpm-repos/vpm.json
sudo -i vrc-get repo add https://hoyotoon.github.io/vpm/index.json
sudo -i vrc-get repo add https://tmyt.github.io/vpm/index.json
sudo -i vrc-get repo add https://rerigferl.github.io/vpm/vpm.json
sudo -i vrc-get repo add https://vrctxl.github.io/VPM/index.json
sudo -i vrc-get repo add https://guribo.github.io/TLP/index.json
sudo -i vrc-get repo add https://cyanlaser.github.io/CyanPlayerObjectPool/index.json
sudo -i vrc-get repo add https://reava.github.io/VPM-Listings/index.json
sudo -i vrc-get repo add https://vavassor.github.io/OrchidSealVPM/index.json
sudo -i vrc-get repo add https://virtualvisions.github.io/VPM-Packages/index.json
sudo -i vrc-get repo add https://vpm.gyoku.tech/vpm.json
sudo -i vrc-get repo add https://pkg-index.yuxiaviation.com/
sudo -i vrc-get repo add https://orels1.github.io/UdonToolkit/index.json
sudo -i vrc-get repo add https://vrcd-community.github.io/vpm-packages/index.json
sudo -i vrc-get repo list

# Resolve VPM Dependencies
sudo -i vrc-get resolve

# Remove Unsupported Packages
sudo -i vrc-get remove dev.foxscore.easy-login -y || true
sudo -i vrc-get remove au.benjithatfoxguy.templates.world.2022.3.22f1.updater -y || true
sudo -i vrc-get remove com.nidonocu.vrcunitytoolbar -y || true
sudo -i vrc-get remove au.benjithatfoxguy.batchimportunitypackage -y || true
sudo -i vrc-get remove au.benjithatfoxguy.restartunity -y || true
sudo -i vrc-get remove dev.hai-vr.resilience.let-me-see -y || true
sudo -i vrc-get remove dev.hai-vr.resilience.toolkit -y || true
sudo -i vrc-get remove unity-editor-dark-mode -y || true
rm -rf Assets/Editor || true
rm -rf Assets/Editor.meta || true
rm -rf Assets/UERP || true
rm -rf Assets/UERP.meta || true

# Add AutoBuild Packages to Workspace
# run: sudo -i vrc-get install com.yuxiaviation.vrchat.autobuild.world 0.3.0 -y
sudo -i vrc-get install com.yuxiaviation.vrchat.autobuild.world -y || true

# Add Patcher Package to fix SDK Build on Linux
sudo -i vrc-get install cn.org.vrcd.vpm.vrchat-sdk-patcher.worlds -y

# Check for required environment variables
if [ -z "$UNITY_EMAIL" ] || [ -z "$UNITY_PASSWORD" ] || [ -z "$UNITY_LICENSE" ]; then
    echo "Error: Required environment variables UNITY_EMAIL, UNITY_PASSWORD, and UNITY_LICENSE must be set"
    exit 1
fi

# Create necessary directories
mkdir -p .secrets

# Create Unity license file
echo "Creating Unity license file from secret..."
echo "$UNITY_LICENSE" > .secrets/Unity_v2023.x.ulf

# Extract serial from license file using the provided script
if [ -f ".github/workflow-scripts/get-serial-from-license-file.js" ]; then
    echo "Extracting Unity serial from license file..."
    UNITY_SERIAL=$(cat .secrets/Unity_v2023.x.ulf | node .github/workflow-scripts/get-serial-from-license-file.js)
    if [ -n "$UNITY_SERIAL" ]; then
        echo "UNITY_SERIAL=$UNITY_SERIAL" > .secrets/serial.env
        echo "Serial extracted successfully"
    else
        echo "Error: Failed to extract serial from license file"
        exit 1
    fi
else
    echo "Error: Serial extraction script not found"
    exit 1
fi

echo "License file created successfully"

# Pull Unity docker image
echo "Pulling Unity docker image..."
docker pull unityci/editor:ubuntu-2022.3.22f1-windows-mono-3.1.0

# Launch Unity in docker to activate the license
echo "Launching Unity to activate license..."
docker run \
    -v "$(pwd):/project" \
    -v "/tmp:/tmp/DefaultCompany/" \
    --rm \
    unityci/editor:ubuntu-2022.3.22f1-windows-mono-3.1.0 \
    unity-editor -projectPath /project \
    -username "$UNITY_EMAIL" \
    -password "$UNITY_PASSWORD" \
    -serial "$UNITY_SERIAL" \
    -batchmode -logFile - \
    -buildTarget Win64 \
    -nographics \
    -quit

echo "Unity setup completed successfully"