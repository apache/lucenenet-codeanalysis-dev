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

## Release 1.0.0-alpha.6

### New Rules

 Rule ID       | Category | Severity | Notes
---------------|----------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------
 LuceneDev1000 | Design   | Warning  | Floating point types should not be compared for exact equality
 LuceneDev1001 | Design   | Warning  | Floating point types should be formatted with J2N methods
 LuceneDev1002 | Design   | Warning  | Floating point type arithmetic needs to be checked
 LuceneDev1003 | Design   | Warning  | Method parameters that accept array types should be analyzed to determine whether they are better suited to be ref or out parameters
 LuceneDev1004 | Design   | Warning  | Methods that return array types should be analyzed to determine whether they are better suited to be one or more out parameters or to return a ValueTuple
