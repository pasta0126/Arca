# ARCA

**ARCA — Administració de Recursos, Claus i Armariets.** Aplicació d’escriptori, lliure i gratuïta, perquè els conserges d’un centre educatiu gestionin les taquilles (armariets), l’alumnat assignat a cadascuna i els pagaments, de manera senzilla i guiada.

> **Estat: en definició.** Les especificacions de la versió 1 i del primera fita estan redactades i validades, però **encara no hi ha codi ni cap versió per descarregar**. Vegeu «Estat del projecte».

## Què resol

Un centre té centenars de taquilles, un alumne per taquilla i cada curs cal saber qui en té una, si ha pagat, qui té la clau i quines estan avariades. ARCA ho recull en una sola aplicació pensada per a persones no tècniques: cada acció dona un resultat clar, i les tasques de risc es fan pas a pas.

## Què inclou la versió 1

- **Taquilles i zones**: alta individual, per rangs o per CSV, estat de cada taquilla (lliure, ocupada, reservada, avariada o en manteniment) i historial.
- **Alumnat i assignacions**: un alumne, una taquilla per curs; alta manual o importació CSV amb revisió prèvia; baixa, canvi i alliberament.
- **Pagaments**: quota anual i dipòsit únic per estada, amb estats (pendent, pagat, exempt, condonat), sense editar ni esborrar: només anul·lar amb motiu. Consulta de pendents de pagament.
- **Claus**: lliurament, retorn i pèrdua amb reposició.
- **Incidències i manteniment**: avaries, manteniments i operacions en bloc.
- **Final de curs guiat**: alliberament de taquilles, devolució de claus, obertura del curs següent i conservació o anonimització de dades.
- **Informes en CSV** i **còpia de seguretat i restauració** manuals, sempre disponibles.
- **Interfície en català**, amb ratolí i teclat, preparada per a altres idiomes.

## Principis

- **Les dades no surten de l’equip.** L’alumnat és menor d’edat: només es recull el mínim, la base de dades va xifrada i no s’envia res a cap servidor. Cap informe ni exportació inclou correu ni identificador.
- **Xifrat amb contrasenya del centre.** Una contrasenya compartida i una clau de recuperació, que es mostra una sola vegada. Sense contrasenya ni clau de recuperació, les dades no es poden recuperar: ningú no en custodia cap còpia.
- **Sense llicències, claus d’activació ni bloquejos.** L’aplicació no deixa mai de funcionar; exportar, fer còpia i restaurar sempre estan disponibles.
- **Cost zero, ara i en el futur.** Només programari i serveis lliures i gratuïts.
- **Multiplataforma**: Windows, Linux i macOS.
- **Fitxers d’intercanvi només CSV.**

## Estat del projecte

| Àrea | Estat |
|------|-------|
| Especificacions (16 canvis, `openspec/changes/`) | Redactades i validades |
| Pantalles de la versió 1 | Definides per a la primera fita (curs, taquilles, alumnat i cobraments) |
| Primera fita (base, cobraments bàsics i cerca) | Abast a `docs/hito-1.md`; implementació encara no iniciada |
| Codi i versions per descarregar | Encara no |
| Validació amb conserges | Pendent |

El projecte es desenvolupa amb OpenSpec (especificació primer, després implementació). Per on començar:

- `AGENTS.md`: instruccions per a persones i eines d’IA que hi treballin.
- `openspec/config.yaml` i `openspec/changes/`: context, decisions i especificacions.
- `docs/hito-1.md` (primera fita), `docs/roadmap.md` (ordre de treball) i `docs/riesgos.md` (riscos i pendents).
- `docs/stack.md`: tecnologies (.NET, Avalonia, SQLite xifrat).

La documentació del projecte és en castellà; la interfície de l’aplicació, en català; el codi, en anglès.

## Registre i avisos (opcionals)

L’aplicació pot, **només si l’usuari ho activa**, registrar la instal·lació i avisar de versions noves. Per defecte no envia res. Vegeu `docs/registro-de-instalaciones.md`.

## Llicència i marca

Programari lliure i gratuït. Llicència: GNU GPL v3.0 o posterior (`GPL-3.0-or-later`, vegeu `LICENSE`). El nom «ARCA» i el seu logotip són marca del titular (vegeu `TRADEMARK.md`): una versió modificada ha de dur un altre nom.

Copyright (c) 2026 Guillermo Garcia Carballo.

## Contribucions i seguretat

Per ara només s’accepten idees, preguntes i avisos, sense codi de tercers: vegeu `CONTRIBUTING.md`. Les vulnerabilitats s’informen en privat: vegeu `SECURITY.md`. Especialment útils: opinions de conserges i de centres sobre com treballen.
