version := "1.6"

@default:
    just --list

# Build the mod DLL
build:
    dotnet build --property "RimWorldVersion={{version}}" ru.pyxiion.modrim.LoadingProgress.sln

# Build and package the mod into PxLoadingProgress.zip
zip: build
    rm -rf package/
    mkdir -p package/About package/Common package/{{version}}/Assemblies
    # About (exclude source files: *.pdn, *.svg, *.ttf)
    cp About/About.xml package/About/
    cp About/ModIcon.png package/About/ 2>/dev/null || true
    cp About/Preview.png package/About/ 2>/dev/null || true
    cp About/PublishedFileId.txt package/About/ 2>/dev/null || true
    cp -r Common/* package/Common/
    cp {{version}}/Assemblies/ru.pyxiion.modrim.loadingprogress.dll package/{{version}}/Assemblies/
    cp LoadFolders.xml package/
    cp CHANGELOG.md package/ 2>/dev/null || true
    cp LICENSE* package/ 2>/dev/null || true
    cp README.md package/ 2>/dev/null || true
    cd package && zip -r ../PxLoadingProgress.zip . && cd ..
    rm -rf package/
    echo "Created PxLoadingProgress.zip"

alias pkg := zip

# Remove build artifacts and package output
clean:
    rm -rf package/ PxLoadingProgress.zip
    dotnet clean --property "RimWorldVersion={{version}}" ru.pyxiion.modrim.LoadingProgress.sln 2>/dev/null || true
