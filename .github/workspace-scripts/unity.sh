# Extract serial from license file using the provided script
UNITY_SERIAL=$(cat .secrets/Unity_v2023.x.ulf | node .github/workflow-scripts/get-serial-from-license-file.js)

# Launch Unity in docker
docker run \
    -v "$(pwd):/project" \
    -v "/tmp:/tmp/DefaultCompany/" \
    --rm \
    -e VRC_AUTO_BUILD_USERNAME="${VRC_AUTO_BUILD_USERNAME}" \
    -e VRC_AUTO_BUILD_PASSWORD="${VRC_AUTO_BUILD_PASSWORD}" \
    -e VRC_AUTO_BUILD_TOTP_KEY="${VRC_AUTO_BUILD_TOTP_KEY}" \
    -e VRC_AUTO_BUILD_SCENE_PATH="Assets\Scenes\VRCDefaultWorldScene.unity" \
    -e VRC_AUTO_BUILD_CONTENT_ID="${VRC_AUTO_BUILD_CONTENT_ID}" \
    unityci/editor:ubuntu-2022.3.22f1-windows-mono-3.1.0 \
    unity-editor -projectPath /project \
    -username "$UNITY_EMAIL" \
    -password "$UNITY_PASSWORD" \
    -serial "$UNITY_SERIAL" \
    -batchmode -logFile - \
    -buildTarget Win64 \
    -nographics \
    -executeMethod VRChatAerospaceUniversity.VRChatAutoBuild.Worlds.AutoBuildVRChatWorld.BuildAndUploadWorld