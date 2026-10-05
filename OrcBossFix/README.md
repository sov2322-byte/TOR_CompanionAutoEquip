# TOR Orc Boss Two-Hand Fix v1.0.0

Target: The Old Realms WITM 1.12 / Bannerlord 1.3.15.

Fixes the compiled WITM1.12 bug in You an' Wot Armour? Keystone:
Armed to da Teef DamageAmount becomes current DamageAmount + round(TwoHanded skill * 0.5).

The patch only intervenes when TOR's original Replace mutation failed and left DamageAmount unchanged, so it will not double-apply if TOR fixes this upstream.

It does not change the base 50 damage, one-handed slow, two-handed -25% Physical Resistance debuff, polearm dismount effect, or any other career perk.

Build target verified against WITM1.12 tag TOR_Core.dll.

Build workflow enabled on main.
