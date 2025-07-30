#!/usr/bin/env python
"""Setup script for installing in development mode"""

import subprocess
import sys
from pathlib import Path

def main():
    """Install the package in development mode"""
    project_root = Path(__file__).parent
    
    # Install the package in editable mode
    cmd = [sys.executable, "-m", "pip", "install", "-e", ".[dev]"]
    
    print("Installing fussball-crawler in development mode...")
    print(f"Command: {' '.join(cmd)}")
    
    result = subprocess.run(cmd, cwd=project_root)
    
    if result.returncode == 0:
        print("\n✅ Installation successful!")
    else:
        print("\n❌ Installation failed!")
        sys.exit(1)

if __name__ == "__main__":
    main()
