{ stdenvNoCC
, fetchurl
, autoPatchelfHook
, icu
, zlib
, openssl
, krb5
, cacert
, tzdata
, stdenv
, lib,
}:

/*
  Minimal "blob-style" .NET SDK package.

  - Fetches the official dotnet SDK tarball from Microsoft
  - Unpacks it into $out
  - Uses autoPatchelfHook + buildInputs to satisfy native deps:
      glibc, libstdc++, libgcc, icu, krb5, openssl, zlib, ca-certificates, tzdata

  Fill in `url` and `sha512` from the official .NET download page.
*/

stdenvNoCC.mkDerivation {
  pname = "dotnet-sdk-blob";
  version = "10.0.100"; # or whatever SDK version you want

  # 1. Fetch the SDK tarball – replace with real URL + sha512
  src = fetchurl {
    # Example pattern only; go to the .NET 10/9/8 download page and copy the real link
    url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.100/dotnet-sdk-10.0.100-linux-x64.tar.gz";
    # Use sha512 since the docs give you that directly; or switch to sha256 if you prefer
    sha512 = "f78dbac30c9af2230d67ff5c224de3a5dbf63f8a78d1c206594dedb80e6909d2cc8a9d865d5105c72c2fd2aa266fc0c6c77dedac60408cbccf272b116bd11b07";
  };

  dontUnpack = true;

  # 2. Patchelf support: patch binaries to use Nix-provided libs
  nativeBuildInputs = [
    autoPatchelfHook
  ];

  # 3. Native deps – this is where your docs mapping lands:
  #
  #   glibc       -> from stdenv (implicit)
  #   libgcc      -> stdenv.cc.cc.lib
  #   libstdc++   -> stdenv.cc.cc.lib
  #   icu         -> icu
  #   krb5        -> krb5
  #   openssl     -> openssl
  #   zlib        -> zlib
  #   ca-certs    -> cacert
  #   tzdata      -> tzdata
  #
  buildInputs = [
    icu
    zlib
    openssl
    krb5
    cacert
    tzdata
    stdenv.cc.cc.lib
  ];

  autoPatchelfIgnoreMissingDeps = [ "liblttng-ust.so.0" ];


  dontStrip = true; # let dotnet keep its own debug info

  # 4. Just unpack the tarball straight into $out
  installPhase = ''
    mkdir -p "$out"
    tar -xzf "$src" -C "$out"

  '';

  # Optional nice-to-have: tell consumers how to use it
  passthru = {
    dotnetRoot = "${placeholder "out"}";
  };

  meta = with lib; {
    description = ".NET SDK blob from official Microsoft tarball (manual-install style)";
    homepage = "https://dotnet.microsoft.com/";
    license  = licenses.mit; # dotnet license is more complex; this is a placeholder
    platforms = platforms.linux;
  };
}
