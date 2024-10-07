# ReactiveDAG

A dynamic directed acyclic graph engine that maintains reactive dependencies, enabling real-time event-driven computations and task execution.

## Overview

A **dynamic, event-driven Directed Acyclic Graph (DAG) computation engine** designed to handle complex, interdependent workflows that need to be updated and recalculated as inputs or conditions change. It manages dependencies between tasks, supports real-time updates, and allows external events or scheduled tasks to trigger computations. 

It functions like a **spreadsheet for computational workflows**—where each node (or cell) represents a function or an input, and any change in the graph propagates to all dependent nodes automatically.

## Key Features

- **Automatic Dependency Management**: Automatically manages dependencies between nodes, ensuring that when an input or condition changes, all related tasks are recalculated in the correct order.
  
- **Dynamic and Real-Time Updates**: Supports real-time data processing, ensuring that dependent nodes are recalculated whenever inputs change, maintaining system consistency.

- **Scheduled Execution**: Supports scheduled task execution at defined intervals, ideal for time-based workflows like monitoring, reporting, and ETL (Extract, Transform, Load) processes.

- **Reactive Computation**: Acts like a reactive data pipeline where updates in the system propagate through all affected tasks and nodes.

## Problem It Solves

It simplifies the management of **complex dependencies** in systems where inputs and tasks change over time or are triggered by external events. It eliminates the need for developers to manually track and manage dependencies, re-executing tasks as necessary, and provides a robust solution for **real-time workflows**, **data pipelines**, and **event-driven architectures**.

### Why Use Reactive DAG?

- **Automatic Recalculation**: Avoid error-prone manual dependency tracking by using an engine that recalculates affected tasks and nodes when inputs change.
  
- **Event-Driven and Scheduled Execution**: Flexibly trigger task execution from external events or define tasks to run at regular intervals.

- **Dynamic and Reactive**: Enable your system to respond to real-time data updates, ensuring that all dependent tasks and nodes reflect the latest state.

## Use Cases

1. **Data Pipelines**: Automate the recalculation of downstream transformations when upstream data changes, ensuring all dependent computations remain accurate.

2. **Event-Driven Applications**: Trigger task execution in response to events such as file uploads, HTTP requests, or database changes, enhancing reactivity.

3. **Scheduled Tasks**: Automatically run periodic tasks like monitoring, reporting, or data aggregation at predefined intervals.

4. **Real-Time Calculations**: Continuously update results as input data changes, ideal for applications like financial modeling, scientific simulations, or dynamic dashboards.

## TODO

1. **Workflow Orchestration**: Let’s say you have several workflows—one for data ingestion, one for processing, and another for reporting. The DagEngine could orchestrate the start, pause, and resumption of each workflow independently. You could also track the progress of each workflow, ensuring that dependencies between nodes are respected and dynamically adjusting the workflow as needed 

2. **Event-Driven Triggers**: Allows external events such as file uploads, HTTP requests, or database updates to trigger the execution of tasks, making the engine flexible and reactive to changes outside the graph.

## Contribution

Please feel free to contribute.
