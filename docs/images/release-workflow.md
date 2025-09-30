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
https://www.mermaidchart.com/app/projects/95759d78-db93-499c-ad66-0e3f698ba88c/diagrams/35faa26e-5ccf-4433-962e-32f20496471c/version/v0.1/edit

```mermaid
flowchart LR
    main[Main Branch]
    release[Release Branch]
    tag[Git Tag]
    draft[Draft Release]
    publish[Publish Release]

    main -->|Prepare Release| release
    release -->|Tag Version| tag
    tag --> draft
    draft -->|Manual Review| publish
```
