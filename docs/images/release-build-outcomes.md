<!--
Licensed to the Apache Software Foundation (ASF) under one
or more contributor license agreements.  See the NOTICE file
distributed with this work for additional information
regarding copyright ownership.  The ASF licenses this file
to you under the Apache License, Version 2.0 (the
"License"); you may not use this file except in compliance
with the License.  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing,
software distributed under the License is distributed on an
"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, either express or implied.  See the License for the
specific language governing permissions and limitations
under the License.
-->

This markup can be edited and converted to .svg or .png here:
https://www.mermaidchart.com/app/projects/95759d78-db93-499c-ad66-0e3f698ba88c/diagrams/31dbd6bc-7ec8-4583-a456-55e3fe3f6cfc/version/v0.1/edit

```mermaid
%%{ init: { "themeVariables": { "fontSize": "24px" } } }%%
flowchart TD
    A["Tag + Push release branch"] --> B{"Draft Release generated?"}
    B -- Yes --> C["Review release notes"]
    C --> D["Check release artifacts"]
    D --> E["Publish Release to NuGet.org"]
    B -- No --> F["Check GitHub Actions logs"]
    F --> G["Fix problems"]
    G --> H["Delete failed tag"]
    H --> I["Reset AnalyzerReleases.Shipped.md<br>header to {{vnext}} if needed"]
    I --> A
```
