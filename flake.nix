{
  description = "Dev shell using manual-install blob-style .NET SDK via Nix";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";

  outputs = { self, nixpkgs }:
    let
      system = "x86_64-linux"; # change if needed
      pkgs   = import nixpkgs { inherit system; };

      # Bring in our blob package from ./dotnet-sdk-blob.nix
      dotnetBlob = pkgs.callPackage ./dotnet-sdk-blob.nix { };

      # Libraries the MS docs say .NET needs on Linux
      runtimeLibs = with pkgs; [
        icu
        zlib
        openssl
        krb5
        lttng-ust
        dotnetCorePackages.sdk_10_0-bin
        vulkan-loader
        vulkan-validation-layers
        vulkan-tools
      ];

      runtimeLibPath = pkgs.lib.makeLibraryPath runtimeLibs;
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        buildInputs = [
          # dotnetBlob
          # add any other tools you like here (nodejs, jq, etc.)
        ] ++ runtimeLibs;

        # Make dotnet usable like in the manual install docs
        shellHook = ''
          # DOTNET_ROOT -> where the SDK lives (like export DOTNET_ROOT=$HOME/.dotnet)
          # export DOTNET_ROOT=${dotnetBlob}
          # PATH -> include DOTNET_ROOT and tools, same idea as docs
          # export PATH="$PATH:${dotnetBlob}:${dotnetBlob}/tools"

          # Make the native libs visible at runtime (ICU, OpenSSL, etc.)
          export LD_LIBRARY_PATH="${runtimeLibPath}:''${LD_LIBRARY_PATH:-}"

          # Vulkan setup
          export VK_LAYER_PATH="${pkgs.vulkan-validation-layers}/share/vulkan/explicit_layer.d"

          # Optional: quick sanity check
          # dotnet --list-sdks || true
        '';
      };
    };
}
