from pathlib import Path
import subprocess
import sys
import tempfile

application = str(Path(sys.argv[1]).resolve())
cases = [
    ("graceful exit", "0\n", ["SPACE STATION CONTROL SYSTEM", "Station system shutting down. Goodbye!"]),
    ("initial telemetry", "1\n0\n", ["LIFE SUPPORT REPORT", "Hab Life Support", "Main Reactor"]),
    ("invalid menu input", "invalid\n0\n", ["Invalid option, please try again."]),
    ("closed input", "", ["Station system shutting down. Goodbye!"]),
]
for name, entries, expected in cases:
    with tempfile.TemporaryDirectory() as working_directory:
        result = subprocess.run(
            [application], input=entries, text=True, capture_output=True,
            cwd=working_directory, timeout=20, errors="replace"
        )
    assert result.returncode == 0, (name, result.stdout, result.stderr)
    for text in expected:
        assert text in result.stdout, (name, text, result.stdout)
    print(f"PASS: {name}")
