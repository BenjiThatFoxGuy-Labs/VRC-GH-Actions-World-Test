UNITY_SERIAL="$(echo '/workspaces/VRC-GH-Actions-World-Test/Unity_v2022.x(1).ulf' | node '/workspaces/VRC-GH-Actions-World-Test/.github/workflow-scripts/get-serial-from-license-file.js')"
echo "::add-mask::$UNITY_SERIAL"
echo $UNITY_SERIAL