/*
 * Drives the single-form request wizard.
 *
 * The form is posted exactly once. Everything between opening the page and submitting it happens
 * here: moving between the three panes, adding and removing exhibits / evidences / inspections, and
 * keeping the strongly typed data cards in step with the structure that decides which model each
 * one is.
 *
 * Rows are never built by hand. Every row and every data card is cloned from a <template> that the
 * server rendered from the same Razor partial the live rows use, under a sentinel prefix
 * (ExhibitTemplate[0], DataTemplates[n]). Cloning therefore preserves the exact markup, including
 * the unobtrusive-validation attributes; all this file does is rewrite the prefix.
 */
(function () {
    'use strict';

    var form = document.querySelector('[data-request-form]');

    if (!form) {
        return;
    }

    var configElement = document.querySelector('[data-request-form-config]');
    var config = configElement ? JSON.parse(configElement.textContent) : {};
    var inspectionTypesByEvidenceType = config.inspectionTypesByEvidenceType || {};
    var evidenceTypeNames = config.evidenceTypeNames || {};
    var combinations = config.combinations || {};

    var exhibitsContainer = form.querySelector('[data-container="exhibits"]');
    var dataContainer = form.querySelector('[data-container="inspection-data"]');
    var noticesContainer = form.querySelector('[data-invalidated-notices]');
    var warningBox = document.querySelector('[data-client-warning]');

    var backButton = form.querySelector('[data-nav="back"]');
    var nextButton = form.querySelector('[data-nav="next"]');
    var submitButton = form.querySelector('[data-nav="submit"]');

    var currentPane = parseInt(form.getAttribute('data-active-pane'), 10) || 1;

    // ---------------------------------------------------------------- helpers

    function rows(container, kind) {
        if (!container) {
            return [];
        }

        return Array.prototype.filter.call(container.children, function (element) {
            return element.getAttribute('data-row') === kind;
        });
    }

    function newId() {
        if (window.crypto && typeof window.crypto.randomUUID === 'function') {
            return window.crypto.randomUUID();
        }

        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = (Math.random() * 16) | 0;
            var v = c === 'x' ? r : ((r & 0x3) | 0x8);
            return v.toString(16);
        });
    }

    // -------------------------------------------------------------- prefixing

    // ASP.NET turns '[', ']' and '.' into '_' when it generates element ids, so a prefix has two
    // written forms in the markup and both have to be rewritten.
    function idForm(name) {
        return name.replace(/[\[\].]/g, '_');
    }

    var NAME_ATTRIBUTES = ['name', 'data-valmsg-for', 'data-prefix'];
    var ID_ATTRIBUTES = ['id', 'for', 'aria-describedby'];

    function setPrefix(element, newPrefix) {
        var oldPrefix = element.getAttribute('data-prefix');

        if (!oldPrefix || oldPrefix === newPrefix) {
            return;
        }

        var oldId = idForm(oldPrefix);
        var newId_ = idForm(newPrefix);

        var nodes = [element].concat(Array.prototype.slice.call(element.querySelectorAll('*')));

        nodes.forEach(function (node) {
            NAME_ATTRIBUTES.forEach(function (attribute) {
                var value = node.getAttribute(attribute);

                if (value && value.indexOf(oldPrefix) === 0) {
                    node.setAttribute(attribute, newPrefix + value.slice(oldPrefix.length));
                }
            });

            ID_ATTRIBUTES.forEach(function (attribute) {
                var value = node.getAttribute(attribute);

                if (value && value.indexOf(oldId) === 0) {
                    node.setAttribute(attribute, newId_ + value.slice(oldId.length));
                }
            });
        });
    }

    /**
     * Renumbers the whole structure pane. Model binding only reads contiguous indices, so this runs
     * after every add and remove rather than trying to patch individual rows - it is also why the
     * old wizard had to redirect after each structural change.
     */
    function reindexStructure() {
        rows(exhibitsContainer, 'exhibit').forEach(function (exhibit, e) {
            setPrefix(exhibit, 'Exhibits[' + e + ']');

            var number = exhibit.querySelector('[data-exhibit-number]');
            var sequence = exhibit.querySelector('[data-field="sequence"]');

            if (number) {
                number.textContent = String(e + 1);
            }

            if (sequence) {
                sequence.value = String(e + 1);
            }

            var evidenceContainer = exhibit.querySelector('[data-container="evidences"]');

            rows(evidenceContainer, 'evidence').forEach(function (evidence, v) {
                setPrefix(evidence, 'Exhibits[' + e + '].Evidences[' + v + ']');

                var inspectionContainer = evidence.querySelector('[data-container="inspections"]');

                rows(inspectionContainer, 'inspection').forEach(function (inspection, i) {
                    setPrefix(
                        inspection,
                        'Exhibits[' + e + '].Evidences[' + v + '].Inspections[' + i + ']');
                });
            });
        });
    }

    // -------------------------------------------------------------- templates

    function cloneTemplate(name, discriminator) {
        var selector = 'template[data-template="' + name + '"]';

        if (discriminator) {
            selector += '[data-discriminator="' + discriminator + '"]';
        }

        var template = document.querySelector(selector);

        return template ? template.content.firstElementChild.cloneNode(true) : null;
    }

    /**
     * The row templates are rendered from a dummy graph that has to be non-empty for the nested
     * templates to exist at all, so a freshly added exhibit or evidence starts without inspections.
     */
    function cloneStructureTemplate(name) {
        var element = cloneTemplate(name);

        if (element && name !== 'inspection') {
            element.querySelectorAll('[data-row="inspection"]').forEach(function (row) {
                row.remove();
            });
        }

        return element;
    }

    // ----------------------------------------------------------- dependent UI

    function inspectionTypeName(evidenceTypeCode, inspectionTypeCode) {
        var types = inspectionTypesByEvidenceType[evidenceTypeCode] || [];

        for (var i = 0; i < types.length; i++) {
            if (types[i].code === inspectionTypeCode) {
                return types[i].displayName;
            }
        }

        return inspectionTypeCode;
    }

    function populateInspectionTypes(select, evidenceTypeCode) {
        var types = inspectionTypesByEvidenceType[evidenceTypeCode] || [];

        select.innerHTML = '';

        var blank = document.createElement('option');
        blank.value = '';
        blank.textContent = '-- select --';
        select.appendChild(blank);

        types.forEach(function (type) {
            var option = document.createElement('option');
            option.value = type.code;
            option.textContent = type.displayName;
            select.appendChild(option);
        });
    }

    function evidenceTypeOf(evidence) {
        var select = evidence.querySelector('[data-select="evidence-type"]');
        return select ? select.value : '';
    }

    function toggleEvidenceInspections(evidence) {
        var hasType = !!evidenceTypeOf(evidence);
        var wrapper = evidence.querySelector('[data-evidence-inspections]');
        var placeholder = evidence.querySelector('[data-empty-evidence-type]');

        if (wrapper) {
            wrapper.hidden = !hasType;
        }

        if (placeholder) {
            placeholder.hidden = hasType;
        }
    }

    // --------------------------------------------------------------- messages

    function showWarning(message) {
        if (!warningBox) {
            return;
        }

        warningBox.textContent = message;
        warningBox.classList.remove('d-none');
    }

    function clearWarning() {
        if (warningBox) {
            warningBox.classList.add('d-none');
            warningBox.textContent = '';
        }
    }

    function renderNotices(notices) {
        if (!noticesContainer) {
            return;
        }

        noticesContainer.innerHTML = '';

        notices.forEach(function (notice) {
            var alert = document.createElement('div');
            alert.className = 'alert alert-warning';
            alert.textContent = notice;
            noticesContainer.appendChild(alert);
        });
    }

    // ------------------------------------------------------- structure events

    form.addEventListener('click', function (event) {
        var button = event.target.closest('button[data-action]');

        if (!button || !form.contains(button)) {
            return;
        }

        clearWarning();

        switch (button.getAttribute('data-action')) {
            case 'add-exhibit':
                addExhibit();
                break;
            case 'remove-exhibit':
                button.closest('[data-row="exhibit"]').remove();
                reindexStructure();
                break;
            case 'add-evidence':
                addEvidence(button.closest('[data-row="exhibit"]'));
                break;
            case 'remove-evidence':
                button.closest('[data-row="evidence"]').remove();
                reindexStructure();
                break;
            case 'add-inspection':
                addInspection(button.closest('[data-row="evidence"]'));
                break;
            case 'remove-inspection':
                button.closest('[data-row="inspection"]').remove();
                reindexStructure();
                break;
            default:
                return;
        }
    });

    function addExhibit() {
        var exhibit = cloneStructureTemplate('exhibit');

        if (!exhibit) {
            return;
        }

        exhibitsContainer.appendChild(exhibit);

        rows(exhibit.querySelector('[data-container="evidences"]'), 'evidence')
            .forEach(toggleEvidenceInspections);

        reindexStructure();
    }

    function addEvidence(exhibit) {
        var evidence = cloneStructureTemplate('evidence');

        if (!evidence) {
            return;
        }

        exhibit.querySelector('[data-container="evidences"]').appendChild(evidence);
        toggleEvidenceInspections(evidence);
        reindexStructure();
    }

    function addInspection(evidence) {
        var inspection = cloneStructureTemplate('inspection');

        if (!inspection) {
            return;
        }

        var idField = inspection.querySelector('[data-field="inspection-id"]');

        if (idField) {
            idField.value = newId();
        }

        var select = inspection.querySelector('[data-select="inspection-type"]');

        if (select) {
            populateInspectionTypes(select, evidenceTypeOf(evidence));
        }

        evidence.querySelector('[data-container="inspections"]').appendChild(inspection);
        reindexStructure();
    }

    form.addEventListener('change', function (event) {
        var evidenceSelect = event.target.closest('[data-select="evidence-type"]');

        if (evidenceSelect) {
            onEvidenceTypeChanged(evidenceSelect);
            return;
        }

        var resultSelect = event.target.closest('[data-chemical-result]');

        if (resultSelect) {
            syncChemicalAnalysis(resultSelect.closest('[data-chemical-analysis]'));
        }
    });

    /**
     * A different evidence type resolves to different inspection models, so the inspections chosen
     * under the old type are no longer valid combinations and go, rather than being silently kept.
     */
    function onEvidenceTypeChanged(select) {
        var evidence = select.closest('[data-row="evidence"]');
        var container = evidence.querySelector('[data-container="inspections"]');
        var removed = rows(container, 'inspection').length;

        container.innerHTML = '';
        toggleEvidenceInspections(evidence);

        if (removed > 0) {
            showWarning(
                'Changing an evidence type removed ' + removed + ' inspection(s), because a ' +
                'different evidence type resolves to different inspection models.');
        }

        reindexStructure();
    }

    // ------------------------------------------------------- inspection data

    function collectConfiguredInspections() {
        var configured = [];

        rows(exhibitsContainer, 'exhibit').forEach(function (exhibit, e) {
            var evidenceContainer = exhibit.querySelector('[data-container="evidences"]');

            rows(evidenceContainer, 'evidence').forEach(function (evidence) {
                var evidenceTypeCode = evidenceTypeOf(evidence);
                var descriptionField = evidence.querySelector('[data-field="description"]');
                var inspectionContainer = evidence.querySelector('[data-container="inspections"]');

                rows(inspectionContainer, 'inspection').forEach(function (row) {
                    var typeSelect = row.querySelector('[data-select="inspection-type"]');
                    var idField = row.querySelector('[data-field="inspection-id"]');
                    var inspectionTypeCode = typeSelect ? typeSelect.value : '';
                    var combination = combinations[evidenceTypeCode + '|' + inspectionTypeCode];

                    if (!combination || !idField) {
                        return;
                    }

                    configured.push({
                        id: idField.value,
                        discriminator: combination.discriminator,
                        exhibitNumber: e + 1,
                        evidenceTypeName: evidenceTypeNames[evidenceTypeCode] || evidenceTypeCode,
                        inspectionTypeName: inspectionTypeName(evidenceTypeCode, inspectionTypeCode),
                        evidenceDescription: descriptionField ? descriptionField.value : ''
                    });
                });
            });
        });

        return configured;
    }

    function applyHeader(card, item) {
        function set(name, value) {
            var element = card.querySelector('[data-header="' + name + '"]');

            if (element) {
                element.textContent = value;
            }
        }

        set('exhibit', String(item.exhibitNumber));
        set('evidence-type', item.evidenceTypeName);
        set('inspection-type', item.inspectionTypeName);
        set('evidence-description', item.evidenceDescription || '');
    }

    /**
     * Reconciles the data pane against the structure. A card whose combination still resolves to the
     * same model is kept as it is, so whatever was typed into it survives a trip back to pane 2;
     * a card whose combination changed is rebuilt from the template and the discarded data is
     * called out rather than quietly disappearing.
     */
    function buildDataPane() {
        var configured = collectConfiguredInspections();
        var existing = {};

        rows(dataContainer, 'data').forEach(function (card) {
            existing[card.getAttribute('data-inspection-id')] = card;
        });

        var notices = [];
        var fragment = document.createDocumentFragment();

        configured.forEach(function (item) {
            var card = existing[item.id];

            if (card && card.getAttribute('data-discriminator') === item.discriminator) {
                delete existing[item.id];
            } else {
                if (card) {
                    delete existing[item.id];
                    notices.push(
                        'Exhibit #' + item.exhibitNumber + ' / ' + item.evidenceTypeName + ' / ' +
                        item.inspectionTypeName + ': previously entered data was discarded because ' +
                        'the inspection configuration changed.');
                }

                card = cloneTemplate('data', item.discriminator);

                if (!card) {
                    return;
                }

                card.setAttribute('data-inspection-id', item.id);
            }

            applyHeader(card, item);
            fragment.appendChild(card);
        });

        // Anything left in `existing` belongs to an inspection that is gone; replaceChildren drops it.
        dataContainer.replaceChildren(fragment);

        rows(dataContainer, 'data').forEach(function (card, k) {
            setPrefix(card, 'Inspections[' + k + ']');

            var idField = card.querySelector('[data-field="inspection-id"]');
            var discriminatorField = card.querySelector('[data-field="discriminator"]');

            if (idField) {
                idField.value = card.getAttribute('data-inspection-id');
            }

            if (discriminatorField) {
                discriminatorField.value = card.getAttribute('data-discriminator');
            }
        });

        renderNotices(notices);
        syncConditionalFields();
        reparseValidation();
    }

    // ------------------------------------------------------ conditional fields

    function syncChemicalAnalysis(container) {
        if (!container) {
            return;
        }

        var select = container.querySelector('[data-chemical-result]');
        var block = container.querySelector('[data-positive-only]');

        if (!select || !block) {
            return;
        }

        var selected = select.options[select.selectedIndex];
        block.style.display = selected && selected.text === 'Positive' ? '' : 'none';
    }

    function syncConditionalFields() {
        form.querySelectorAll('[data-chemical-analysis]').forEach(syncChemicalAnalysis);
    }

    // ------------------------------------------------------------ validation

    function reparseValidation() {
        if (!window.jQuery || !window.jQuery.validator || !window.jQuery.validator.unobtrusive) {
            return;
        }

        var $form = window.jQuery(form);
        $form.removeData('validator').removeData('unobtrusiveValidation');
        window.jQuery.validator.unobtrusive.parse($form);
    }

    function validateVisibleFields(scope) {
        if (!window.jQuery || !window.jQuery.fn.valid) {
            return true;
        }

        var valid = true;

        window.jQuery(scope).find('input, select, textarea').each(function () {
            if (this.name && !window.jQuery(this).valid()) {
                valid = false;
            }
        });

        return valid;
    }

    /** The same rules the server enforces in CreateModel.ValidateStructure; this one is convenience. */
    function structureProblems() {
        var problems = [];
        var exhibits = rows(exhibitsContainer, 'exhibit');
        var total = 0;

        if (!exhibits.length) {
            problems.push('Add at least one exhibit.');
        }

        exhibits.forEach(function (exhibit, e) {
            var evidences = rows(exhibit.querySelector('[data-container="evidences"]'), 'evidence');

            if (!evidences.length) {
                problems.push('Exhibit #' + (e + 1) + ': add at least one evidence.');
            }

            evidences.forEach(function (evidence) {
                var evidenceTypeCode = evidenceTypeOf(evidence);

                if (!evidenceTypeCode) {
                    problems.push('Exhibit #' + (e + 1) + ': every evidence needs an evidence type.');
                    return;
                }

                var inspections = rows(
                    evidence.querySelector('[data-container="inspections"]'), 'inspection');

                if (!inspections.length) {
                    problems.push(
                        'Exhibit #' + (e + 1) + ': evidence needs at least one inspection.');
                }

                inspections.forEach(function (row) {
                    var select = row.querySelector('[data-select="inspection-type"]');

                    if (!select || !select.value) {
                        problems.push(
                            'Exhibit #' + (e + 1) + ': every inspection needs an inspection type.');
                        return;
                    }

                    total++;
                });
            });
        });

        if (total === 0) {
            problems.push('Add at least one inspection.');
        }

        return problems.filter(function (problem, index, all) {
            return all.indexOf(problem) === index;
        });
    }

    // ------------------------------------------------------------ navigation

    function showPane(pane) {
        currentPane = pane;

        form.querySelectorAll('[data-pane]').forEach(function (section) {
            section.classList.toggle(
                'd-none',
                parseInt(section.getAttribute('data-pane'), 10) !== pane);
        });

        document.querySelectorAll('[data-wizard-steps] [data-step]').forEach(function (item) {
            var step = parseInt(item.getAttribute('data-step'), 10);
            var state = step === pane
                ? 'bg-primary text-white'
                : step < pane
                    ? 'bg-success-subtle text-success-emphasis'
                    : 'bg-body-secondary text-body-secondary';

            item.className = 'px-3 py-2 rounded ' + state;
        });

        backButton.classList.toggle('d-none', pane === 1);
        nextButton.classList.toggle('d-none', pane === 3);
        submitButton.classList.toggle('d-none', pane !== 3);

        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    nextButton.addEventListener('click', function () {
        clearWarning();

        if (currentPane === 1) {
            if (!validateVisibleFields(form.querySelector('[data-pane="1"]'))) {
                return;
            }

            showPane(2);
            return;
        }

        if (currentPane === 2) {
            var problems = structureProblems();

            if (problems.length) {
                showWarning(problems.join(' '));
                return;
            }

            buildDataPane();
            showPane(3);
        }
    });

    backButton.addEventListener('click', function () {
        clearWarning();

        if (currentPane > 1) {
            showPane(currentPane - 1);
        }
    });

    // ---------------------------------------------------------------- startup

    rows(exhibitsContainer, 'exhibit').forEach(function (exhibit) {
        rows(exhibit.querySelector('[data-container="evidences"]'), 'evidence')
            .forEach(toggleEvidenceInspections);
    });

    syncConditionalFields();
    showPane(currentPane);
})();
