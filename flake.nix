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
        shader-slang
        glfw
        trashy
        stdenv.cc.cc.lib  # libstdc++ for native NuGet libraries
        nodejs_24
	docfx
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
          # Make the native libs visible at runtime (ICU, OpenSSL, etc.)
          export LD_LIBRARY_PATH="${runtimeLibPath}:''${LD_LIBRARY_PATH:-}"

          # Enable dynamic loading of native libraries from NuGet packages
          export NIX_LD_LIBRARY_PATH="${runtimeLibPath}:''${NIX_LD_LIBRARY_PATH:-}"
          export NIX_LD="$(cat ${pkgs.stdenv.cc}/nix-support/dynamic-linker)"

          # Vulkan setup
          export VK_LAYER_PATH="${pkgs.vulkan-validation-layers}/share/vulkan/explicit_layer.d"
        '';
      };
    };
}
