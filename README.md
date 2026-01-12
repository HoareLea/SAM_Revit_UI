[![Build (Windows)](https://github.com/SAM-BIM/SAM_Revit_UI/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/SAM-BIM/SAM_Revit_UI/actions/workflows/build.yml)
[![Installer (latest)](https://img.shields.io/github/v/release/SAM-BIM/SAM_Deploy?label=installer)](https://github.com/SAM-BIM/SAM_Deploy/releases/latest)

# SAM_Revit_UI

<a href="https://github.com/SAM-BIM/SAM">
  <img src="https://github.com/SAM-BIM/SAM/blob/master/Grasshopper/SAM.Core.Grasshopper/Resources/SAM_Small.png"
       align="left" hspace="10" vspace="6">
</a>

**SAM_Revit_UI** is part of the **SAM (Sustainable Analytical Model) Toolkit** —  
an open-source collection of tools designed to help engineers create, manage,
and process analytical building models for energy and environmental analysis.

This repository provides a **user-facing Revit add-in (`.addin`)**
that enables SAM workflows to be executed directly within **Autodesk Revit**
through a **more accessible, click-based user interface**.

The add-in is intended to simplify access to SAM functionality,
reducing the need for scripting or visual programming,
while remaining fully compatible with the underlying SAM analytical workflows.

Welcome — and let’s keep the open-source journey going. 🤝

---

## Requirements

- **Autodesk Revit 2025 or later**
- **SAM Toolkit** (installed via the SAM installer)

---

## Installing

To install **SAM**, download and run the  
[latest Windows installer](https://github.com/HoareLea/SAM_Deploy/releases).

The installer will deploy the required SAM components,
including the **SAM_Revit_UI add-in**, for supported Revit versions.

---

## Documentation

Please refer to the **SAM Wiki** for detailed guidance,
setup instructions, and usage examples for the Revit UI workflows:

📘 https://github.com/SAM-BIM/SAM/wiki

---

## Resources
- 🧠 **SAM Core:** https://github.com/SAM-BIM/SAM  
- 🧩 **SAM_Revit:** https://github.com/SAM-BIM/SAM_Revit  
- 🧰 **SAM Installer:** https://github.com/HoareLea/SAM_Deploy/releases  

---

## Development notes

- Target framework: **.NET / C#**
- UI follows SAM-BIM interaction and workflow conventions
- Designed for a user-friendly, click-driven experience
- New or modified `.cs` files must include the SPDX header from `COPYRIGHT_HEADER.txt`

---

## Licence

This repository is free software licensed under the  
**GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later)**.

Each contributor retains copyright to their respective contributions.  
The project history (Git) records authorship and provenance of all changes.

See:
- `LICENSE`
- `NOTICE`
- `COPYRIGHT_HEADER.txt`
