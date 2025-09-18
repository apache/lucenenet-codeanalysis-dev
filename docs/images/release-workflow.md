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
