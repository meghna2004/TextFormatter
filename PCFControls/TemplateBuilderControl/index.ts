 //import { IInputs, IOutputs } from "./generated/ManifestTypes";

// export class TemplateBuilderControl implements ComponentFramework.StandardControl<IInputs, IOutputs> {
//     /**
//      * Empty constructor.
//      */
//     constructor() {
//         // Empty
//     }

//     /**
//      * Used to initialize the control instance. Controls can kick off remote server calls and other initialization actions here.
//      * Data-set values are not initialized here, use updateView.
//      * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to property names defined in the manifest, as well as utility functions.
//      * @param notifyOutputChanged A callback method to alert the framework that the control has new outputs ready to be retrieved asynchronously.
//      * @param state A piece of data that persists in one session for a single user. Can be set at any point in a controls life cycle by calling 'setControlState' in the Mode interface.
//      * @param container If a control is marked control-type='standard', it will receive an empty div element within which it can render its content.
//      */
//     public init(
//         context: ComponentFramework.Context<IInputs>,
//         notifyOutputChanged: () => void,
//         state: ComponentFramework.Dictionary,
//         container: HTMLDivElement
//     ): void {
//         // Add control initialization code
//     }


//     /**
//      * Called when any value in the property bag has changed. This includes field values, data-sets, global values such as container height and width, offline status, control metadata values such as label, visible, etc.
//      * @param context The entire property bag available to control via Context Object; It contains values as set up by the customizer mapped to names defined in the manifest, as well as utility functions
//      */
//     public updateView(context: ComponentFramework.Context<IInputs>): void {
//         // Add code to update control view
//     }

//     /**
//      * It is called by the framework prior to a control receiving new data.
//      * @returns an object based on nomenclature defined in manifest, expecting object[s] for property marked as "bound" or "output"
//      */
//     public getOutputs(): IOutputs {
//         return {};
//     }

//     /**
//      * Called when the control is to be removed from the DOM tree. Controls should use this call for cleanup.
//      * i.e. cancelling any pending remote calls, removing listeners, etc.
//      */
//     public destroy(): void {
//         // Add code to cleanup control if necessary
//     }
// }
import { IInputs, IOutputs } from "./generated/ManifestTypes";
interface SubSection {
    name: string;
    columns: string[];
  }
  
  interface Query {
    entityName: string;
    filter: string;
    subSections: SubSection[];
  }
  
  interface Section {
    sectionName: string;
    queries: Query[];
  }
  
  interface TemplateConfig {
    sections: Section[];
  }
  
export class TemplateBuilderControl implements ComponentFramework.StandardControl<IInputs, IOutputs> {

    private container: HTMLDivElement;
    private context: ComponentFramework.Context<IInputs>;
    private notifyOutputChanged: () => void;
    private config: {
        sections: Section[];
      } = {
        sections: []
      };
      
   // private config: any = { sections: [] }; // Your main structure


    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this.context = context;
        this.notifyOutputChanged = notifyOutputChanged;
        this.container = container;

        this.renderUI();
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        this.context = context;
        this.renderUI();
    }

    public renderUI() : void {
        if (!this.container) {
            console.error("Container is undefined");
            return;
        }
        this.container.innerHTML = "";

        const addSectionBtn = document.createElement("button");
        addSectionBtn.innerText = "+ Add Section";
        addSectionBtn.onclick = () => {
            this.config.sections.push({ sectionName: "", queries: [] });
            this.notifyOutputChanged();
            this.renderUI();
        };

        this.container.appendChild(addSectionBtn);

        this.config.sections.forEach((section: Section, sectionIndex: number): void => {
            const sectionDiv = document.createElement("div");
            sectionDiv.style.border = "1px solid #ccc";
            sectionDiv.style.margin = "10px";
            sectionDiv.style.padding = "10px";

            const sectionInput = document.createElement("input");
            sectionInput.placeholder = "Section Name";
            sectionInput.value = section.sectionName;
            sectionInput.onchange = (e) => {
                section.sectionName = (e.target as HTMLInputElement).value;
                this.notifyOutputChanged();
            };
            sectionDiv.appendChild(sectionInput);

            const addQueryBtn = document.createElement("button");
            addQueryBtn.innerText = "+ Add Query";
            addQueryBtn.onclick = () => {
                section.queries.push({ entityName: "", filter: "", subSections: [] });
                this.notifyOutputChanged();
                this.renderUI();
            };
            sectionDiv.appendChild(addQueryBtn);

            section.queries.forEach((query: Query, queryIndex: number) => {
                const queryDiv = document.createElement("div");
                queryDiv.style.border = "1px dashed #999";
                queryDiv.style.margin = "10px";
                queryDiv.style.padding = "10px";

                const entityInput = document.createElement("input");
                entityInput.placeholder = "Entity Name";
                entityInput.value = query.entityName;
                entityInput.onchange = (e) => {
                    query.entityName = (e.target as HTMLInputElement).value;
                    this.notifyOutputChanged();
                };
                queryDiv.appendChild(entityInput);

                const filterInput = document.createElement("input");
                filterInput.placeholder = "Filter (optional)";
                filterInput.value = query.filter;
                filterInput.onchange = (e) => {
                    query.filter = (e.target as HTMLInputElement).value;
                    this.notifyOutputChanged();
                };
                queryDiv.appendChild(filterInput);

                const addSubBtn = document.createElement("button");
                addSubBtn.innerText = "+ Add Subsection";
                addSubBtn.onclick = () => {
                    query.subSections.push({ name: "", columns: [] });
                    this.notifyOutputChanged();
                    this.renderUI();
                };
                queryDiv.appendChild(addSubBtn);

                query.subSections.forEach((sub: SubSection, subIndex: number) => {
                    const subDiv = document.createElement("div");
                    subDiv.style.border = "1px dotted #666";
                    subDiv.style.margin = "10px";
                    subDiv.style.padding = "10px";

                    const subNameInput = document.createElement("input");
                    subNameInput.placeholder = "Subsection Name";
                    subNameInput.value = sub.name;
                    subNameInput.onchange = (e) => {
                        sub.name = (e.target as HTMLInputElement).value;
                        this.notifyOutputChanged();
                    };
                    subDiv.appendChild(subNameInput);

                    const columnInput = document.createElement("input");
                    columnInput.placeholder = "Comma-separated column names";
                    columnInput.value = sub.columns.join(", ");
                    columnInput.onchange = (e) => {
                        const raw = (e.target as HTMLInputElement).value;
                        sub.columns = raw.split(",").map((s) => s.trim());
                        this.notifyOutputChanged();
                    };
                    subDiv.appendChild(columnInput);

                    queryDiv.appendChild(subDiv);
                });

                sectionDiv.appendChild(queryDiv);
            });

            this.container.appendChild(sectionDiv);
        });
    }

    public getOutputs(): IOutputs {
        return {
            configJson: JSON.stringify(this.config),
        };
    }

    public destroy(): void {
        // clean up if needed
    }
}
