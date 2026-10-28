#!/usr/bin/env python3
"""Regression fixtures for the coverage ratchet; synthetic inventory only."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

spec = spec_from_file_location('qa_coverage_gate', Path(__file__).with_name('qa_coverage_gate.py'))
gate = module_from_spec(spec)
spec.loader.exec_module(gate)

inventory = {
    'api': [
        {'method': 'GET', 'route': '/api/v1/a', 'backendTestReference': True, 'access': 'AUTHORIZED'},
        {'method': 'POST', 'route': '/api/v1/b', 'backendTestReference': False, 'access': 'AUTHORIZED'},
        {'method': 'GET', 'route': '/api/public/v1/c', 'backendTestReference': False, 'access': 'PUBLIC_IMPLICIT'},
    ],
    'webPages': [
        {'route': '/dues', 'features': ['dues']},
        {'route': '/new', 'features': ['brand-new']},
        {'route': '/redirect', 'features': []},
    ],
}
current = gate.gaps(inventory, "import x from '../src/features/dues/model.ts'")
assert current == {'untestedEndpoints': ['POST /api/v1/b'], 'pagesWithoutWebTest': ['/new']}, current
added, closed = gate.compare(current, {'untestedEndpoints': ['POST /api/v1/b', 'GET /api/v1/old'], 'pagesWithoutWebTest': []})
assert added == {'untestedEndpoints': [], 'pagesWithoutWebTest': ['/new']}, added
assert closed == {'untestedEndpoints': ['GET /api/v1/old'], 'pagesWithoutWebTest': []}, closed
print('PASS: coverage ratchet 3/3 fixtures; repository not scanned.')
