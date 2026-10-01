#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Copyright (c) 2026 Guillermo Garcia Carballo
#
# Signs the Windows executables of a folder with a SELF-SIGNED certificate (demos and pilots only, see docs/firma-de-codigo.md):
#   build/sign-local.sh <folder>
# Needs osslsigncode (brew install osslsigncode) and openssl. The first time it creates the certificate in
# ~/.arca-signing (outside the repository: the private key never goes into the repo or its history).
# Besides signing, it leaves in <folder> the public certificate (ARCA-certificat.cer) and the script that imports it
# on a Windows computer (Instal-lar-certificat.cmd, needs an administrator): until it is imported, Windows
# still treats the signature as from an unknown publisher.
# No timestamp server is used (nothing goes off the computer): the signature stops being valid when the certificate expires.
set -euo pipefail
cd "$(dirname "$0")/.."

dir="${1:?usage: build/sign-local.sh <folder with Arca.exe>}"
[[ -f "$dir/Arca.exe" ]] || { echo "No Arca.exe in $dir" >&2; exit 2; }
command -v osslsigncode >/dev/null || { echo "osslsigncode is missing: brew install osslsigncode" >&2; exit 2; }

home="${ARCA_SIGNING_DIR:-$HOME/.arca-signing}"
if [[ ! -f "$home/arca-signing.key" ]]; then
  mkdir -p "$home" && chmod 700 "$home"
  cat > "$home/openssl.cnf" <<CNF
[req]
distinguished_name = dn
x509_extensions = ext
prompt = no
[dn]
CN = ARCA (certificat propi, nomes demostracio)
O = ARCA
[ext]
basicConstraints = critical,CA:FALSE
keyUsage = critical,digitalSignature
extendedKeyUsage = codeSigning
subjectKeyIdentifier = hash
CNF
  openssl req -x509 -newkey rsa:3072 -sha256 -days 1825 -nodes -config "$home/openssl.cnf" \
    -keyout "$home/arca-signing.key" -out "$home/arca-signing.crt" 2>/dev/null
  chmod 600 "$home/arca-signing.key"
  echo "Certificate created in $home (valid 5 years). Back up that folder if you want to keep the same publisher."
fi

for exe in "$dir/Arca.exe"; do
  osslsigncode sign -certs "$home/arca-signing.crt" -key "$home/arca-signing.key" \
    -h sha256 -n "ARCA" -in "$exe" -out "$exe.signed"
  mv "$exe.signed" "$exe"
done
osslsigncode verify -in "$dir/Arca.exe" 2>&1 | grep -E 'Signature|Number of signers|Subject' | head -4 || true

openssl x509 -in "$home/arca-signing.crt" -outform der -out "$dir/ARCA-certificat.cer"
# ASCII only and CRLF line ends: it is read by Windows cmd.
printf '%s\r\n' \
  '@echo off' \
  'rem Imports the ARCA certificate as trusted for this computer. Run it with a right click, "Run as administrator".' \
  'net session >nul 2>&1' \
  'if errorlevel 1 (' \
  '  echo Cal executar aquest fitxer com a administrador: clic dret i "Executa com a administrador".' \
  '  pause' \
  '  exit /b 1' \
  ')' \
  'certutil -addstore -f Root "%~dp0ARCA-certificat.cer"' \
  'certutil -addstore -f TrustedPublisher "%~dp0ARCA-certificat.cer"' \
  'echo.' \
  'echo Fet. ARCA ja es considera un editor de confianca en aquest equip.' \
  'pause' > "$dir/Instal-lar-certificat.cmd"
echo "OK: signed $dir/Arca.exe"
