.PHONY: build check-arch check-assets check-config config-export process-assets test-slice py-env bk

build:
	dotnet build Phagocyte.csproj --warnaserror

check-arch:
	python3 tools/check_arch.py

check-config:
	cd tools/config && npm run check

config-export:
	cd tools/config && npm run build

check-assets:
	python3 tools/asset_check/main.py

process-assets:
	python3 tools/to_target_asset/main.py --no-clean

test-slice:
	python3 tools/slice/test_slice.py

py-env:
	python3 -m venv .venv && .venv/bin/pip install -r tools/requirements.txt

bk:
	git add .
	git commit -m "update"
	git push