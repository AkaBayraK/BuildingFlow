#!/usr/bin/env python3
"""Regression fixtures for the QA inventory matcher; synthetic paths only, no repository scan."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

spec = spec_from_file_location('qa_inventory', Path(__file__).with_name('qa_inventory.py'))
inventory = module_from_spec(spec)
spec.loader.exec_module(inventory)

normalize, matches, prefix_of = inventory.normalize, inventory.matches, inventory.prefix_of
checks = [
    normalize('/api/v1/properties/${encodeURIComponent(id)}/dues?page=1') == '/api/v1/properties/{}/dues',
    normalize('/api/v1/properties/{propertyId:guid}/dues/') == '/api/v1/properties/{}/dues',
    normalize('/api/v2/tenants/${t}/people${id ? `/${id}` : ""}') == '/api/v2/tenants/{}/people',
    matches('/api/v1/properties/{}/dues', '/api/v1/properties/{}/dues'),
    not matches('/api/v1/properties/{}/dues', '/api/v1/properties/{}/payments'),
    not matches('/api/v1/properties/{}', '/api/v1/properties/{}/dues'),
    matches('/api/v1/payments/{}/receipt', '/api/v1/payments/{}/receipt'),
    prefix_of('/api/v1/consents/types', '/api/v1/consents/types/{}/texts'),
    not prefix_of('/api/v1', '/api/v1/mobile/sync/commands'),
    not prefix_of('/api/v1/consents/types', '/api/v1/consents/types'),
    inventory.tab_visibility('<Tabs.Screen name="a" options={{ title: \'A\' }} /><Tabs.Screen name="b" options={{ href: linked ? undefined : null, title: \'B\' }} /><Tabs.Screen name="c" options={{href:null,title:"C"}} />')
    == [{'tab': 'a', 'visibility': 'ALWAYS'}, {'tab': 'b', 'visibility': 'LINKED_CONTEXT'}, {'tab': 'c', 'visibility': 'HIDDEN'}],
]
failed = [index for index, passed in enumerate(checks, 1) if not passed]
assert not failed, f'failed fixtures: {failed}'
print(f'PASS: QA inventory matcher {len(checks)}/{len(checks)} fixtures; repository not scanned.')
